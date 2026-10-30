using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Godot.Collections;
using Microsoft.Extensions.Logging;
using PVZHE.ModEditor.ModSystem;
using ZLogger;

[GlobalClass]
public partial class TowerDefenseManager : Node2D
{
	private sealed class FallingObjectReplicationCapture : IDisposable
	{
		private TowerDefenseManager _owner;

		private readonly FallingObjectReplicationCapture _parent;

		private readonly List<FallingObjectSpawnRecord> _records = new List<FallingObjectSpawnRecord>();

		public FallingObjectReplicationCapture(TowerDefenseManager owner, FallingObjectReplicationCapture parent)
		{
			_owner = owner;
			_parent = parent;
			owner._activeFallingObjectReplicationCapture = this;
		}

		public void Record(TowerDefenseGroundItemBase item, ObjectManagerConfig.OBJECT objectId, Vector2 position, double height, Vector2 velocity, double gravity)
		{
			_records.Add(new FallingObjectSpawnRecord
			{
				Item = item,
				ObjectId = objectId,
				Position = position,
				Height = height,
				Velocity = velocity,
				Gravity = gravity
			});
		}

		public void Dispose()
		{
			TowerDefenseManager owner = _owner;
			_owner = null;
			if (GodotObject.IsInstanceValid(owner))
			{
				if (owner._activeFallingObjectReplicationCapture == this)
				{
					owner._activeFallingObjectReplicationCapture = _parent;
				}
				owner.FlushFallingObjectSpawns(_records);
			}
		}
	}

	private sealed class FallingObjectSpawnRecord
	{
		public TowerDefenseGroundItemBase Item;

		public ObjectManagerConfig.OBJECT ObjectId;

		public Vector2 Position;

		public double Height;

		public Vector2 Velocity;

		public double Gravity;
	}

	private static PackedScene _towerDefenseAwardPacket;

	private static PackedScene _towerDefenseAwardPurse;

	private static PackedScene _towerDefenseAwardCollectable;

	private static PackedScene _towerDefenseAwardTrophy;

	private static PackedScene _towerDefenseZombieBungiSpawn;

	private static PackedScene _towerDefenseEffectParticlesOnce;

	private static PackedScene _towerDefenseEffectSpriteOnce;

	private FallingObjectReplicationCapture _activeFallingObjectReplicationCapture;

	private static FallingObjectConfig _zombieDeathFallingObject;

	private static readonly StringName FeatureName_PacketBank = new StringName("PacketBank");

	private static readonly StringName FeatureName_Portal = new StringName("Portal");

	private static readonly StringName FeatureName_SeedBank = new StringName("SeedBank");

	private static readonly StringName FeatureName_RainMode = new StringName("RainMode");

	private static readonly StringName FeatureName_ConveyorBelt = new StringName("ConveyorBelt");

	private static readonly StringName FeatureName_Glove = new StringName("Glove");

	private static readonly StringName FeatureName_ScreenEffect = new StringName("ScreenEffect");

	private static readonly StringName FeatureName_Sun = new StringName("Sun");

	private static readonly StringName FeatureName_Map = new StringName("Map");

	private static readonly StringName FeatureName_Mower = new StringName("Mower");

	private static readonly StringName FeatureName_Brain = new StringName("Brain");

	private static readonly ILogger _logger = Log.CreateLogger<TowerDefenseManager>();

	private static TowerDefenseBattleFeatureMap _cachedMapFeature = null;

	private static TowerDefenseControlNew _cachedMapFeatureControl = null;

	private static PackedScene _towerDefenseInGamePacketShow;

	internal const int MaxDeathRecordsPerCamp = 512;

	private static readonly TowerDefenseDeathHistory DeathHistory = new TowerDefenseDeathHistory(512);

	private System.Collections.Generic.Dictionary<string, Godot.Collections.Array> _groupCache = new System.Collections.Generic.Dictionary<string, Godot.Collections.Array>();

	private long _groupCacheFrame = -1L;

	private readonly System.Collections.Generic.Dictionary<string, int> _characterCountByName = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);

	private long _characterCountFrame = -1L;

	private ulong _characterCountQueryRevision = 18446744073709551615uL;

	private static System.Collections.Generic.Dictionary<string, TowerDefensePacketConfig> _packetConfigRefCache = new System.Collections.Generic.Dictionary<string, TowerDefensePacketConfig>();

	private static System.Collections.Generic.Dictionary<string, TowerDefenseProjectileConfig> _projectileConfigRefCache = new System.Collections.Generic.Dictionary<string, TowerDefenseProjectileConfig>();

	public BattleEventBus eventBus;

	public TowerDefenseBattleCharacterRegistry characterRegistry;

	public TargetSystem targetSystem;

	public DamagePipeline damagePipeline;

	private CoinBank _coinBank;

	public TowerDefenseControlNew currentControl;

	[Export(PropertyHint.None, "")]
	public TowerDefenseLevelBaseConfig currentLevelConfig;

	public int currentDynamicLevel = 3;

	public int seedbankPacketMax = 7;

	public double runGameTime;

	public Vector2 gridSize;

	public Vector2 gridBeginPos;

	public Vector2I gridNum;

	public bool pausePacket;

	public bool pauseZombie;

	public bool backPacket;

	public bool backZombie;

	private static PackedScene _towerDefenseSun;

	private static PackedScene _towerDefenseBrainSun;

	private static PackedScene TOWER_DEFENSE_AWARD_PACKET => _towerDefenseAwardPacket ?? (_towerDefenseAwardPacket = GD.Load<PackedScene>("uid://7l7qsvsxvioi"));

	private static PackedScene TOWER_DEFENSE_AWARD_PURSE => _towerDefenseAwardPurse ?? (_towerDefenseAwardPurse = GD.Load<PackedScene>("uid://mux1v63kv0d8"));

	private static PackedScene TOWER_DEFENSE_AWARD_COLLECTABLE => _towerDefenseAwardCollectable ?? (_towerDefenseAwardCollectable = GD.Load<PackedScene>("uid://d28midqtre16s"));

	private static PackedScene TOWER_DEFENSE_AWARD_TROPHY => _towerDefenseAwardTrophy ?? (_towerDefenseAwardTrophy = GD.Load<PackedScene>("uid://dpoujenmcn5tb"));

	private static PackedScene TOWER_DEFENSE_ZOMBIE_BUNGI_SPAWN => _towerDefenseZombieBungiSpawn ?? (_towerDefenseZombieBungiSpawn = GD.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Scene/TowerDefenseZombieBungiSpawn.tscn"));

	private static PackedScene TOWER_DEFENSE_EFFECT_PARTICLES_ONCE => _towerDefenseEffectParticlesOnce ?? (_towerDefenseEffectParticlesOnce = GD.Load<PackedScene>("uid://dbyd0mqkya1j3"));

	private static PackedScene TOWER_DEFENSE_EFFECT_SPRITE_ONCE => _towerDefenseEffectSpriteOnce ?? (_towerDefenseEffectSpriteOnce = GD.Load<PackedScene>("uid://dwvgduivkprow"));

	private static FallingObjectConfig ZOMBIE_DEATH_FALLING_OBJECT => _zombieDeathFallingObject ?? (_zombieDeathFallingObject = GD.Load<FallingObjectConfig>("uid://ct867xau74s6u"));

	private static PackedScene TOWER_DEFENSE_IN_GAME_PACKET_SHOW => _towerDefenseInGamePacketShow ?? (_towerDefenseInGamePacketShow = GD.Load<PackedScene>("uid://bhqecss20rwpb"));

	internal static int DeathRecordCount => DeathHistory.Count;

	public CoinBank coinBank => _coinBank;

	public static bool HasGameplayAuthority
	{
		get
		{
			if (Global.IsMultiplayerMode)
			{
				return MultiPlayerManager.IsHost;
			}
			return true;
		}
	}

	public static TowerDefenseManager Instance { get; private set; }

	public static TowerDefenseControlNew CurrentControl
	{
		get
		{
			if (Instance == null)
			{
				return null;
			}
			return Instance.currentControl;
		}
	}

	private static PackedScene TOWER_DEFENSE_SUN => _towerDefenseSun ?? (_towerDefenseSun = GD.Load<PackedScene>("uid://dk3bkihnh1i0l"));

	private static PackedScene TOWER_DEFENSE_BRAIN_SUN => _towerDefenseBrainSun ?? (_towerDefenseBrainSun = GD.Load<PackedScene>("uid://d161xee5m0kkw"));

	public TowerDefenseAwardBase CreateAward(TowerDefenseEnum.LEVEL_REWARDTYPE type, string itemName, Vector2 pos)
	{
		return type switch
		{
			TowerDefenseEnum.LEVEL_REWARDTYPE.NOONE => CreateAwardFromScene(TOWER_DEFENSE_AWARD_PURSE, itemName, pos), 
			TowerDefenseEnum.LEVEL_REWARDTYPE.PACKET => CreateAwardFromScene(TOWER_DEFENSE_AWARD_PACKET, itemName, pos), 
			TowerDefenseEnum.LEVEL_REWARDTYPE.COLLECTABLE => CreateAwardFromScene(TOWER_DEFENSE_AWARD_COLLECTABLE, itemName, pos), 
			TowerDefenseEnum.LEVEL_REWARDTYPE.COIN => CreateAwardFromScene(TOWER_DEFENSE_AWARD_PURSE, itemName, pos), 
			TowerDefenseEnum.LEVEL_REWARDTYPE.TROPHY => CreateAwardFromScene(TOWER_DEFENSE_AWARD_TROPHY, itemName, pos), 
			_ => null, 
		};
	}

	private TowerDefenseAwardBase CreateAwardFromScene(PackedScene awardScene, string itemName, Vector2 pos)
	{
		Node node = awardScene.Instantiate(PackedScene.GenEditState.Disabled);
		GetCharacterNode().AddChild(node, forceReadableName: false, InternalMode.Disabled);
		((Node2D)node).GlobalPosition = pos;
		((TowerDefenseAwardBase)node).Init(itemName);
		return (TowerDefenseAwardBase)node;
	}

	public TowerDefenseBackgroundMusicConfig GetbackgroundMusicConfig(string backgroundMusic)
	{
		TowerDefenseBackgroundMusicConfig result = null;
		ResourceManager.Instance?.EnsureAllBgmsLoaded();
		if (!string.IsNullOrEmpty(backgroundMusic) && ResourceManager.Instance != null && ResourceManager.Instance.BGMS.ContainsKey(backgroundMusic))
		{
			result = (TowerDefenseBackgroundMusicConfig)ResourceManager.Instance.BGMS[backgroundMusic];
		}
		return result;
	}

	public TowerDefenseSunBase SunCreate(Vector2 pos, long sunNum, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.LAND, double height = 0.0, Vector2 velocity = default(Vector2), double gravity = 980.0, double moveStopTime = -1.0)
	{
		return CreateSunDrop((IsIZMMode() || IsIZM2Mode()) ? ObjectManagerConfig.OBJECT.SUN_BRAIN : ObjectManagerConfig.OBJECT.SUN, EconomyAccountId.Local, SunDropOwnershipPolicy.LegacySharedReplica, pos, sunNum, movingMethod, height, velocity, gravity, moveStopTime);
	}

	public TowerDefenseSunBase SunCreate(EconomyAccountId accountId, Vector2 pos, long sunNum, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.LAND, double height = 0.0, Vector2 velocity = default(Vector2), double gravity = 980.0, double moveStopTime = -1.0)
	{
		if (!CanCreateAccountOwnedSunDrop(accountId))
		{
			return null;
		}
		return CreateSunDrop((IsIZMMode() || IsIZM2Mode()) ? ObjectManagerConfig.OBJECT.SUN_BRAIN : ObjectManagerConfig.OBJECT.SUN, accountId, SunDropOwnershipPolicy.AccountOwned, pos, sunNum, movingMethod, height, velocity, gravity, moveStopTime);
	}

	public Godot.Collections.Array GetCampFriendlyFromArea(TowerDefenseEnum.CHARACTER_CAMP camp, AabbArea2D checkArea, bool fliterGraveStone = true)
	{
		Rect2 checkRect = AabbShapeUtil.ComputeAreaWorldRect(checkArea);
		List<TowerDefenseCharacter> charactersIntersectingRectList = characterRegistry.GetCharactersIntersectingRectList(checkRect);
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (TowerDefenseCharacter item in charactersIntersectingRectList)
		{
			if (!item.die && !item.nearDie && !(item is TowerDefenseCrater) && !(item is TowerDefenseItem) && (!fliterGraveStone || !(item is TowerDefenseGravestone)) && item.camp == camp)
			{
				array.Add(item);
			}
		}
		return array;
	}

	public Godot.Collections.Array GetCampFriendlyFromArea(TowerDefenseEnum.CHARACTER_CAMP camp, IAabbAreaQuery2D checkArea, bool fliterGraveStone = true)
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		if (checkArea == null || !checkArea.TryGetWorldRect(out var rect))
		{
			return array;
		}
		foreach (TowerDefenseCharacter charactersIntersectingRect in characterRegistry.GetCharactersIntersectingRectList(rect))
		{
			if (!charactersIntersectingRect.die && !charactersIntersectingRect.nearDie && !(charactersIntersectingRect is TowerDefenseCrater) && !(charactersIntersectingRect is TowerDefenseItem) && (!fliterGraveStone || !(charactersIntersectingRect is TowerDefenseGravestone)) && charactersIntersectingRect.camp == camp)
			{
				array.Add(charactersIntersectingRect);
			}
		}
		return array;
	}

	public Godot.Collections.Array GetCampFriendlyLine(TowerDefenseCharacter character, bool fliterGraveStone = true)
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (TowerDefenseCharacter item in _GetCleanCharacters())
		{
			if (!(item is TowerDefenseCrater) && !(item is TowerDefenseItem) && (!fliterGraveStone || !(item is TowerDefenseGravestone)) && item.camp == character.camp && item.gridPos.Y == character.gridPos.Y)
			{
				array.Add(item);
			}
		}
		return array;
	}

	public Godot.Collections.Array GetCampFriendly(TowerDefenseEnum.CHARACTER_CAMP camp, bool fliterGraveStone = true)
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (TowerDefenseCharacter item in _GetCleanCharacters())
		{
			if (!item.die && !item.nearDie && !(item is TowerDefenseCrater) && !(item is TowerDefenseItem) && (!fliterGraveStone || !(item is TowerDefenseGravestone)) && item.camp == camp)
			{
				array.Add(item);
			}
		}
		return array;
	}

	public Godot.Collections.Array GetCampTarget(TowerDefenseEnum.CHARACTER_CAMP camp, bool fliterGraveStone = true)
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (TowerDefenseCharacter item in _GetCleanCharacters())
		{
			if (!item.die && !item.nearDie && !(item is TowerDefenseCrater) && !(item is TowerDefenseItem) && (!fliterGraveStone || !(item is TowerDefenseGravestone)) && item.camp != camp)
			{
				array.Add(item);
			}
		}
		return array;
	}

	public Godot.Collections.Array GetCampTargetFromArray(AabbArea2D checkArea, TowerDefenseEnum.CHARACTER_CAMP camp, bool fliterGraveStone = true)
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		Rect2 checkRect = AabbShapeUtil.ComputeAreaWorldRect(checkArea);
		foreach (TowerDefenseCharacter charactersIntersectingRect in characterRegistry.GetCharactersIntersectingRectList(checkRect))
		{
			if (!charactersIntersectingRect.die && !charactersIntersectingRect.nearDie && charactersIntersectingRect.camp != camp && !(charactersIntersectingRect is TowerDefenseCrater) && !(charactersIntersectingRect is TowerDefenseItem) && (!fliterGraveStone || !(charactersIntersectingRect is TowerDefenseGravestone)))
			{
				array.Add(charactersIntersectingRect);
			}
		}
		return array;
	}

	private int CountConveyorPacketsByCharacterName(string characterName)
	{
		int num = 0;
		TowerDefenseBattleFeatureConveyorBelt conveyorBeltFeature = GetConveyorBeltFeature();
		if (GodotObject.IsInstanceValid(conveyorBeltFeature))
		{
			foreach (Node packetChild in conveyorBeltFeature.GetPacketChildren())
			{
				if ((packetChild as TowerDefenseInGamePacketShow).config.saveKey == characterName)
				{
					num++;
				}
			}
		}
		return num;
	}

	private int GetCachedCharacterCount(string characterName)
	{
		if (!GodotObject.IsInstanceValid(characterRegistry))
		{
			return 0;
		}
		long physicsFrames = (long)Engine.GetPhysicsFrames();
		ulong queryRevision = characterRegistry.QueryRevision;
		if (_characterCountFrame != physicsFrames || _characterCountQueryRevision != queryRevision)
		{
			RefreshCharacterCountCache(physicsFrames, queryRevision);
		}
		if (characterName == null || !_characterCountByName.TryGetValue(characterName, out var value))
		{
			return 0;
		}
		return value;
	}

	private void RefreshCharacterCountCache(long currentFrame, ulong currentRevision)
	{
		_characterCountFrame = currentFrame;
		_characterCountQueryRevision = currentRevision;
		_characterCountByName.Clear();
		List<TowerDefenseCharacter> cleanCharactersList = characterRegistry.GetCleanCharactersList();
		for (int i = 0; i < cleanCharactersList.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = cleanCharactersList[i];
			if (!towerDefenseCharacter.inGame || towerDefenseCharacter.config == null)
			{
				continue;
			}
			string name = towerDefenseCharacter.config.name;
			if (!string.IsNullOrEmpty(name))
			{
				if (_characterCountByName.TryGetValue(name, out var value))
				{
					_characterCountByName[name] = value + 1;
				}
				else
				{
					_characterCountByName[name] = 1;
				}
			}
		}
	}

	public int GetCharacterNum(string characterName, bool containConveyor = false)
	{
		int num = GetCachedCharacterCount(characterName);
		if (containConveyor)
		{
			num += CountConveyorPacketsByCharacterName(characterName);
		}
		return num;
	}

	public Godot.Collections.Array GetCharacterFromName(string characterName)
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (TowerDefenseCharacter item in _GetCleanCharacters())
		{
			if (item.inGame && item.config.name == characterName)
			{
				array.Add(item);
			}
		}
		return array;
	}

	public TowerDefenseBattleFeatureRainMode GetRainModeFeature()
	{
		TowerDefenseControlNew towerDefenseControlNew = currentControl;
		if (towerDefenseControlNew != null)
		{
			return towerDefenseControlNew.GetFeature(FeatureName_RainMode) as TowerDefenseBattleFeatureRainMode;
		}
		return null;
	}

	public TowerDefenseBattleFeatureConveyorBelt GetConveyorBeltFeature()
	{
		if (currentControl == null)
		{
			return null;
		}
		if (currentControl.GetFeature(FeatureName_ConveyorBelt) is TowerDefenseBattleFeatureConveyorBelt result)
		{
			return result;
		}
		return null;
	}

	public TowerDefenseBattleFeatureGlove GetGloveFeature()
	{
		TowerDefenseControlNew towerDefenseControlNew = currentControl;
		if (towerDefenseControlNew != null)
		{
			return towerDefenseControlNew.GetFeature(FeatureName_Glove) as TowerDefenseBattleFeatureGlove;
		}
		return null;
	}

	public TowerDefenseBattleFeatureScreenEffect GetScreenEffectFeature()
	{
		TowerDefenseControlNew towerDefenseControlNew = currentControl;
		if (towerDefenseControlNew != null)
		{
			return towerDefenseControlNew.GetFeature(FeatureName_ScreenEffect) as TowerDefenseBattleFeatureScreenEffect;
		}
		return null;
	}

	public TowerDefenseBattleFeatureBrain GetBrainFeature()
	{
		TowerDefenseControlNew towerDefenseControlNew = currentControl;
		if (towerDefenseControlNew != null)
		{
			return towerDefenseControlNew.GetFeature(FeatureName_Brain) as TowerDefenseBattleFeatureBrain;
		}
		return null;
	}

	public TowerDefenseBattleProcess GetCurrentProcess()
	{
		if (currentControl == null)
		{
			return null;
		}
		return currentControl.process;
	}

	public Godot.Collections.Array GetPlant()
	{
		return _GetNodesInGroupCached("Plant");
	}

	public Godot.Collections.Array GetZombie()
	{
		return _GetNodesInGroupCached("Zombie");
	}

	public Godot.Collections.Array GetCharacter()
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (TowerDefenseCharacter item in _GetCleanCharacters())
		{
			if (!item.characterFilter)
			{
				array.Add(item);
			}
		}
		return array;
	}

	public Godot.Collections.Array GetProjectile()
	{
		return _GetNodesInGroupCached("Projectile");
	}

	public Godot.Collections.Array GetEffect()
	{
		return _GetNodesInGroupCached("Effect");
	}

	public int GetEffectCount()
	{
		return characterRegistry.GetEffectCount();
	}

	internal int GetEffectCountForPhysicsFrame(ulong physicsFrame)
	{
		return characterRegistry.GetEffectCountForPhysicsFrame(physicsFrame);
	}

	public Godot.Collections.Array GetLineCharacters(int line)
	{
		return characterRegistry.GetLineCharacters(line);
	}

	public List<TowerDefenseCharacter> GetCharacterColumn(int column, bool fliterGraveStone = true)
	{
		return targetSystem.GetCharacterColumn(column, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetCharacterColumnList(int column, bool fliterGraveStone = true)
	{
		return targetSystem.GetCharacterColumnList(column, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetFarFromArray(TowerDefenseCharacter character, List<TowerDefenseCharacter> array, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false)
	{
		return targetSystem.GetCharacterTargetFarFromArray(character, array, method, checkLine, fliterGravestone);
	}

	public bool GetCharacterHasTarget(TowerDefenseCharacter character, bool checkLine = false, bool fliterGraveStone = true)
	{
		return targetSystem.GetCharacterHasTarget(character, checkLine, fliterGraveStone);
	}

	public bool GetCharacterHasTargetFromArray(TowerDefenseCharacter character, List<TowerDefenseCharacter> array, bool checkLine = false, bool fliterGraveStone = true)
	{
		return targetSystem.GetCharacterHasTargetFromArray(character, array, checkLine, fliterGraveStone);
	}

	public bool GetCharacterHasTargetFromArea(TowerDefenseCharacter character, AabbArea2D checkArea, bool checkLine = false, bool fliterGraveStone = true)
	{
		return targetSystem.GetCharacterHasTargetFromArea(character, checkArea, checkLine, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetLineWithCollisionFlags(TowerDefenseCharacter character, int collisionFlags, bool fliterGraveStone = true)
	{
		return targetSystem.GetCharacterTargetLineWithCollisionFlags(character, collisionFlags, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetLineFromArea(TowerDefenseCharacter character, AabbArea2D checkArea, bool fliterGraveStone = true)
	{
		return targetSystem.GetCharacterTargetLineFromArea(character, checkArea, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetLineFromAreaWithCollisionFlags(TowerDefenseCharacter character, int collisionFlags, AabbArea2D checkArea, bool fliterGraveStone = true)
	{
		return targetSystem.GetCharacterTargetLineFromAreaWithCollisionFlags(character, collisionFlags, checkArea, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetFromAreaWithCollisionFlags(TowerDefenseCharacter character, int collisionFlags, AabbArea2D checkArea, bool fliterGraveStone = true)
	{
		return targetSystem.GetCharacterTargetFromAreaWithCollisionFlags(character, collisionFlags, checkArea, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetCharacterLine(int line, bool fliterGraveStone = true)
	{
		return targetSystem.GetCharacterLine(line, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetCharacterLineList(int line, bool fliterGraveStone = true)
	{
		return targetSystem.GetCharacterLineList(line, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetLine(TowerDefenseCharacter character, bool fliterGraveStone = true)
	{
		return targetSystem.GetCharacterTargetLine(character, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetLineList(TowerDefenseCharacter character, bool fliterGraveStone = true)
	{
		return targetSystem.GetCharacterTargetLineList(character, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetLineFromArray(TowerDefenseCharacter character, List<TowerDefenseCharacter> array, bool fliterGraveStone = true)
	{
		return targetSystem.GetCharacterTargetLineFromArray(character, array, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetCharacterTarget(TowerDefenseCharacter character, bool checkLine = false, bool checkCollision = false, bool fliterGraveStone = true)
	{
		return targetSystem.GetCharacterTarget(character, checkLine, checkCollision, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetList(TowerDefenseCharacter character, bool checkLine = false, bool checkCollision = false, bool fliterGraveStone = true)
	{
		return targetSystem.GetCharacterTargetList(character, checkLine, checkCollision, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetNearFromArea(TowerDefenseCharacter character, AabbArea2D checkArea, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false)
	{
		return targetSystem.GetCharacterTargetNearFromArea(character, checkArea, method, checkLine, fliterGravestone);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetFarFromArrayWithCollisionFlags(TowerDefenseCharacter character, int collisionFlags, List<TowerDefenseCharacter> array, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false)
	{
		return targetSystem.GetCharacterTargetFarFromArrayWithCollisionFlags(character, collisionFlags, array, method, checkLine, fliterGravestone);
	}

	public TowerDefenseCharacter GetCharacterTargetNearestFromArrayWithCollisionFlags(TowerDefenseCharacter character, int collisionFlags, List<TowerDefenseCharacter> array, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false)
	{
		return targetSystem.GetCharacterTargetNearestFromArrayWithCollisionFlags(character, collisionFlags, array, method, checkLine, fliterGravestone);
	}

	public TowerDefenseCharacter GetCharacterTargetFarthestFromArrayWithCollisionFlags(TowerDefenseCharacter character, int collisionFlags, List<TowerDefenseCharacter> array, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false)
	{
		return targetSystem.GetCharacterTargetFarthestFromArrayWithCollisionFlags(character, collisionFlags, array, method, checkLine, fliterGravestone);
	}

	public TowerDefenseCharacter GetCharacterTargetNearest(TowerDefenseCharacter character, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false)
	{
		return targetSystem.GetCharacterTargetNearest(character, method, checkLine, fliterGravestone);
	}

	public TowerDefenseCharacter GetCharacterTargetNearestFromArray(TowerDefenseCharacter character, List<TowerDefenseCharacter> array, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false)
	{
		return targetSystem.GetCharacterTargetNearestFromArray(character, array, method, checkLine, fliterGravestone);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetNear(TowerDefenseCharacter character, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false)
	{
		return targetSystem.GetCharacterTargetNear(character, method, checkLine, fliterGravestone);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetNearFromArray(TowerDefenseCharacter character, List<TowerDefenseCharacter> array, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false)
	{
		return targetSystem.GetCharacterTargetNearFromArray(character, array, method, checkLine, fliterGravestone);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetNearFromArrayWithCollisionFlags(TowerDefenseCharacter character, int collisionFlags, List<TowerDefenseCharacter> array, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false)
	{
		return targetSystem.GetCharacterTargetNearFromArrayWithCollisionFlags(character, collisionFlags, array, method, checkLine, fliterGravestone);
	}

	public TowerDefenseCharacter GetNearCharacter(TowerDefenseCharacter character, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false, bool fliterVase = true)
	{
		return targetSystem.GetNearCharacter(character, method, checkLine, fliterGravestone, fliterVase);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetFromRectWithCollisionFlags(TowerDefenseCharacter character, int collisionFlags, Rect2 checkRect, bool fliterGraveStone = true)
	{
		return targetSystem.GetCharacterTargetFromRectWithCollisionFlags(character, collisionFlags, checkRect, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetLineFromRectWithCollisionFlags(TowerDefenseCharacter character, int collisionFlags, Rect2 checkRect, bool fliterGraveStone = true)
	{
		return targetSystem.GetCharacterTargetLineFromRectWithCollisionFlags(character, collisionFlags, checkRect, fliterGraveStone);
	}

	public TowerDefenseCharacter GetTallCharacterTargetFromRect(TowerDefenseCharacter character, Rect2 checkRect, bool checkLine = false, int line = 0, double groundRight = 1.0 / 0.0)
	{
		return targetSystem.GetTallCharacterTargetFromRect(character, checkRect, checkLine, line, groundRight);
	}

	public List<TowerDefenseCharacter> GetCharactersIntersectingRect(Rect2 checkRect, bool checkLine = false, int line = 0)
	{
		return targetSystem.GetCharactersIntersectingRect(checkRect, checkLine, line);
	}

	public List<TowerDefenseCharacter> GetOverlappingCharactersFromRect(Rect2 checkRect, bool checkLine = false, int line = 0)
	{
		return GetCharactersIntersectingRect(checkRect, checkLine, line);
	}

	public TowerDefenseCharacter GetCharacterTargetNearestFromRect(TowerDefenseCharacter character, Rect2 checkRect, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool fliterGravestone = false)
	{
		return targetSystem.GetCharacterTargetNearestFromRect(character, checkRect, method, fliterGravestone);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetFromRect(TowerDefenseCharacter character, Rect2 checkRect, bool checkLine = false, bool fliterGraveStone = true, bool fliterVase = true)
	{
		return targetSystem.GetCharacterTargetFromRect(character, checkRect, checkLine, fliterGraveStone, fliterVase);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetFromRectList(TowerDefenseCharacter character, Rect2 checkRect, bool checkLine = false, bool fliterGraveStone = true, bool fliterVase = true)
	{
		return targetSystem.GetCharacterTargetFromRectList(character, checkRect, checkLine, fliterGraveStone, fliterVase);
	}

	public bool GetCharacterHasTargetFromRect(TowerDefenseCharacter character, Rect2 checkRect, bool checkLine = false, bool fliterGraveStone = true, bool fliterVase = true)
	{
		return targetSystem.GetCharacterHasTargetFromRect(character, checkRect, checkLine, fliterGraveStone, fliterVase);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetFromArray(TowerDefenseCharacter character, List<TowerDefenseCharacter> array, bool checkLine = false, bool fliterGraveStone = true)
	{
		return targetSystem.GetCharacterTargetFromArray(character, array, checkLine, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetFromArrayWithCollisionFlags(TowerDefenseCharacter character, int collisionFlags, List<TowerDefenseCharacter> array, bool checkLine = false, bool fliterGraveStone = true)
	{
		return targetSystem.GetCharacterTargetFromArrayWithCollisionFlags(character, collisionFlags, array, checkLine, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetFromArea(TowerDefenseCharacter character, AabbArea2D checkArea, bool checkLine = false, bool fliterGraveStone = true, bool fliterVase = true)
	{
		return targetSystem.GetCharacterTargetFromArea(character, checkArea, checkLine, fliterGraveStone, fliterVase);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetFromAreaList(TowerDefenseCharacter character, AabbArea2D checkArea, bool checkLine = false, bool fliterGraveStone = true, bool fliterVase = true)
	{
		return targetSystem.GetCharacterTargetFromAreaList(character, checkArea, checkLine, fliterGraveStone, fliterVase);
	}

	public bool HasTrackTarget(TowerDefenseCharacter parent, int collectionFlag, bool canTargetGargantuar, float groundRight)
	{
		return targetSystem.HasTrackTarget(parent, collectionFlag, canTargetGargantuar, groundRight);
	}

	public TowerDefenseCharacter GetCharacterTargetNearestFromArea(TowerDefenseCharacter character, AabbArea2D checkArea, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool fliterGravestone = false)
	{
		return targetSystem.GetCharacterTargetNearestFromArea(character, checkArea, method, fliterGravestone);
	}

	public Godot.Collections.Array GetCharactersForLine(int line)
	{
		return characterRegistry.GetCharactersForLine(line);
	}

	public bool GetProjectileHasTarget(TowerDefenseProjectile projectile, bool checkLine = false, bool fliterGraveStone = true)
	{
		return targetSystem.GetProjectileHasTarget(projectile, checkLine, fliterGraveStone);
	}

	public bool GetProjectileHasTargetFromArray(TowerDefenseProjectile projectile, List<TowerDefenseCharacter> array, bool checkLine = false, bool fliterGraveStone = true)
	{
		return targetSystem.GetProjectileHasTargetFromArray(projectile, array, checkLine, fliterGraveStone);
	}

	public bool GetProjectileHasTargetFromArea(TowerDefenseProjectile projectile, AabbArea2D checkArea, bool checkLine = false, bool fliterGraveStone = true)
	{
		return targetSystem.GetProjectileHasTargetFromArea(projectile, checkArea, checkLine, fliterGraveStone);
	}

	public TowerDefenseCharacter GetProjectileTargetNearest(TowerDefenseProjectile projectile, int collisionFlags = -1, bool fliterGravestone = true)
	{
		return targetSystem.GetProjectileTargetNearest(projectile, collisionFlags, fliterGravestone);
	}

	public TowerDefenseCharacter GetProjectileTargetNearest(Vector2 pos, Vector2I gridPos, int collisionFlags, TowerDefenseEnum.CHARACTER_CAMP camp, double speed, bool fliterGravestone = true, bool deprioritizeDisabledTargets = false)
	{
		return targetSystem.GetProjectileTargetNearest(pos, gridPos, collisionFlags, camp, speed, fliterGravestone, 18446744073709551615uL, deprioritizeDisabledTargets);
	}

	public TowerDefenseCharacter GetProjectileTargetNearestForPhysicsFrame(Vector2 pos, Vector2I gridPos, int collisionFlags, TowerDefenseEnum.CHARACTER_CAMP camp, double speed, ulong physicsFrame, bool fliterGravestone = true, bool deprioritizeDisabledTargets = false)
	{
		return targetSystem.GetProjectileTargetNearest(pos, gridPos, collisionFlags, camp, speed, fliterGravestone, physicsFrame, deprioritizeDisabledTargets);
	}

	public List<TowerDefenseCharacter> GetProjectileTargetNear(TowerDefenseProjectile projectile, int collisionFlags = -1, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false)
	{
		return targetSystem.GetProjectileTargetNear(projectile, collisionFlags, method, checkLine, fliterGravestone);
	}

	public List<TowerDefenseCharacter> GetProjectileTargetNearProjectile(TowerDefenseProjectile projectile, int collisionFlags = -1, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = true)
	{
		return targetSystem.GetProjectileTargetNearProjectile(projectile, collisionFlags, method, checkLine, fliterGravestone);
	}

	public List<TowerDefenseCharacter> GetProjectileTarget(TowerDefenseProjectile projectile, bool checkLine = false, bool fliterGraveStone = true)
	{
		return targetSystem.GetProjectileTarget(projectile, checkLine, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetProjectileTargetList(TowerDefenseProjectile projectile, bool checkLine = false, bool fliterGraveStone = true)
	{
		return targetSystem.GetProjectileTargetList(projectile, checkLine, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetProjectileTargetFromArray(TowerDefenseProjectile projectile, List<TowerDefenseCharacter> array, bool checkLine = false, bool fliterGraveStone = true)
	{
		return targetSystem.GetProjectileTargetFromArray(projectile, array, checkLine, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetProjectileTargetFromArea(TowerDefenseProjectile projectile, AabbArea2D checkArea, bool checkLine = false, bool fliterGraveStone = true)
	{
		return targetSystem.GetProjectileTargetFromArea(projectile, checkArea, checkLine, fliterGraveStone);
	}

	public TowerDefenseCharacter GetProjectileTargetNearestProjectile(TowerDefenseProjectile projectile, int collisionFlags = -1, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool fliterGravestone = true)
	{
		return targetSystem.GetProjectileTargetNearestProjectile(projectile, collisionFlags, method, fliterGravestone);
	}

	public TowerDefenseCharacter GetProjectileInitialTrackTarget(Vector2 projectilePos, int collisionFlags, TowerDefenseEnum.CHARACTER_CAMP camp, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.POSITION, bool fliterGravestone = true)
	{
		return targetSystem.GetProjectileInitialTrackTarget(projectilePos, collisionFlags, camp, method, fliterGravestone);
	}

	public TowerDefenseSunBase BrainSunCreate(Vector2 pos, long sunNum, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.LAND, double height = 0.0, Vector2 velocity = default(Vector2), double gravity = 980.0, double moveStopTime = -1.0)
	{
		return CreateSunDrop(ObjectManagerConfig.OBJECT.SUN_BRAIN, EconomyAccountId.Local, SunDropOwnershipPolicy.LegacySharedReplica, pos, sunNum, movingMethod, height, velocity, gravity, moveStopTime);
	}

	public TowerDefenseSunBase BrainSunCreate(EconomyAccountId accountId, Vector2 pos, long sunNum, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.LAND, double height = 0.0, Vector2 velocity = default(Vector2), double gravity = 980.0, double moveStopTime = -1.0)
	{
		if (!CanCreateAccountOwnedSunDrop(accountId))
		{
			return null;
		}
		return CreateSunDrop(ObjectManagerConfig.OBJECT.SUN_BRAIN, accountId, SunDropOwnershipPolicy.AccountOwned, pos, sunNum, movingMethod, height, velocity, gravity, moveStopTime);
	}

	public TowerDefenseZombie BungiSpawn(string packetName, Vector2I gridPos, TowerDefenseCharacterOverride override_ = null, bool hypnoses = false, int waveOperationId = -1, bool skipPlacementCheck = false)
	{
		if (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			return null;
		}
		TowerDefenseBattleFeatureWave instance = TowerDefenseBattleFeatureWave.Instance;
		if (!IsBungiWaveOperationCurrent(waveOperationId, instance))
		{
			return null;
		}
		if (!TryGetBungiSpawnContext(out var battleControl, out var characterNode))
		{
			return null;
		}
		int battleOperationId = battleControl.BeginPendingBattleOperation();
		int ownedWaveOperationId = (GodotObject.IsInstanceValid(instance) ? instance.BeginPendingSpawnOperation() : (-1));
		TowerDefenseZombieBungiSpawn towerDefenseZombieBungiSpawn = null;
		try
		{
			towerDefenseZombieBungiSpawn = CreateBungiSpawnNode(packetName, gridPos, override_, instance, ownedWaveOperationId, battleControl, battleOperationId, characterNode);
		}
		catch (Exception value)
		{
			CleanupFailedBungiSpawn(battleControl, battleOperationId, instance, ownedWaveOperationId, towerDefenseZombieBungiSpawn);
			GD.PushError($"[BungiSpawn] Failed to create {packetName}: {value}");
			return null;
		}
		if (hypnoses)
		{
			towerDefenseZombieBungiSpawn.Hypnoses();
		}
		SyncBungiSpawn(packetName, gridPos, hypnoses, towerDefenseZombieBungiSpawn, battleControl, skipPlacementCheck);
		return towerDefenseZombieBungiSpawn;
	}

	private static bool IsBungiWaveOperationCurrent(int waveOperationId, TowerDefenseBattleFeatureWave waveFeature)
	{
		if (waveOperationId < 0)
		{
			return true;
		}
		if (GodotObject.IsInstanceValid(waveFeature))
		{
			return waveFeature.IsPendingSpawnOperationCurrent(waveOperationId);
		}
		return false;
	}

	private bool TryGetBungiSpawnContext(out TowerDefenseControlNew battleControl, out Node characterNode)
	{
		battleControl = currentControl;
		characterNode = GetCharacterNode();
		if (!GodotObject.IsInstanceValid(battleControl) || !GodotObject.IsInstanceValid(characterNode))
		{
			return false;
		}
		if (GodotObject.IsInstanceValid(battleControl.levelControl) && battleControl.levelControl.awardCreate)
		{
			return false;
		}
		return true;
	}

	private static TowerDefenseZombieBungiSpawn CreateBungiSpawnNode(string packetName, Vector2I gridPos, TowerDefenseCharacterOverride override_, TowerDefenseBattleFeatureWave waveFeature, int ownedWaveOperationId, TowerDefenseControlNew battleControl, int battleOperationId, Node characterNode)
	{
		TowerDefenseZombieBungiSpawn towerDefenseZombieBungiSpawn = TOWER_DEFENSE_ZOMBIE_BUNGI_SPAWN.Instantiate<TowerDefenseZombieBungiSpawn>(PackedScene.GenEditState.Disabled);
		towerDefenseZombieBungiSpawn.Set("characterName", packetName);
		towerDefenseZombieBungiSpawn.Set("override", override_);
		towerDefenseZombieBungiSpawn.waveOperationOwner = waveFeature;
		towerDefenseZombieBungiSpawn.waveOperationId = ownedWaveOperationId;
		towerDefenseZombieBungiSpawn.battleOperationOwner = battleControl;
		towerDefenseZombieBungiSpawn.battleOperationId = battleOperationId;
		towerDefenseZombieBungiSpawn.SetLogicalGlobalPosition(GetMapCellPlantPos(gridPos));
		towerDefenseZombieBungiSpawn.gridPos = gridPos;
		characterNode.AddChild(towerDefenseZombieBungiSpawn, forceReadableName: false, InternalMode.Disabled);
		return towerDefenseZombieBungiSpawn;
	}

	private static void CleanupFailedBungiSpawn(TowerDefenseControlNew battleControl, int battleOperationId, TowerDefenseBattleFeatureWave waveFeature, int ownedWaveOperationId, TowerDefenseZombieBungiSpawn zombie)
	{
		battleControl.CompletePendingBattleOperation(battleOperationId);
		if (ownedWaveOperationId >= 0 && GodotObject.IsInstanceValid(waveFeature))
		{
			waveFeature.CompletePendingSpawnOperation(ownedWaveOperationId);
		}
		if (GodotObject.IsInstanceValid(zombie) && !zombie.IsQueuedForDeletion())
		{
			zombie.QueueFree();
		}
	}

	private static void SyncBungiSpawn(string packetName, Vector2I gridPos, bool hypnoses, TowerDefenseZombieBungiSpawn zombie, TowerDefenseControlNew battleControl, bool skipPlacementCheck)
	{
		if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			int nextSyncId = battleControl.GetNextSyncId();
			double hitpointScale = (GodotObject.IsInstanceValid(zombie.instance) ? zombie.instance.hitpointScale : 1.0);
			double scaleVal = (GodotObject.IsInstanceValid(zombie.transformPoint) ? ((double)zombie.transformPoint.Scale.X) : 1.0);
			Dictionary spawnState = (skipPlacementCheck ? new Dictionary { ["plant_skip_placement_check"] = true } : null);
			battleControl.RegisterSyncCharacter(nextSyncId, zombie);
			MultiPlayerManager.Instance.SendSpawnCharacterAt(packetName, gridPos.X, gridPos.Y, nextSyncId, hitpointScale, scaleVal, hypnoses, 0.0, useCreate: false, 0.0, 0.0, walkAfterSpawn: false, 0.0, "", spawnState);
		}
	}

	public static Node2D GetCharacterNode()
	{
		if (Instance == null)
		{
			return null;
		}
		if (Global.Instance.isEditor && SceneManager.CurrentScene == "LevelEditorStage" && GodotObject.IsInstanceValid(LevelEditorMapEditor.instance))
		{
			return LevelEditorMapEditor.instance.characterNode;
		}
		if (!GodotObject.IsInstanceValid(Instance.currentControl))
		{
			return ObjectManager.Instance;
		}
		return Instance.currentControl.characterNode;
	}

	public void CharacterRegister(TowerDefenseCharacter character)
	{
		characterRegistry.Register(character);
	}

	public void CharacterUnregister(TowerDefenseCharacter character)
	{
		characterRegistry.Unregister(character);
	}

	public static AdobeAnimateSprite GetCharacterSprite(string charcterSpriteName)
	{
		PackedScene characterSprite = ResourceManager.Instance.GetCharacterSprite(charcterSpriteName);
		if (!GodotObject.IsInstanceValid(characterSprite))
		{
			return null;
		}
		return characterSprite.Instantiate(PackedScene.GenEditState.Disabled) as AdobeAnimateSprite;
	}

	public static PackedScene GetPacketSpriteScene(TowerDefensePacketConfig config)
	{
		if (!GodotObject.IsInstanceValid(config) || ResourceManager.Instance == null)
		{
			return null;
		}
		if (!string.IsNullOrEmpty(config.saveKey) && ResourceManager.Instance.CHARCTAER_SPRITE.TryGetValue(config.saveKey, out var value) && value is PackedScene result)
		{
			return result;
		}
		TowerDefenseCharacterConfig characterConfig = config.characterConfig;
		if (!GodotObject.IsInstanceValid(characterConfig) || string.IsNullOrEmpty(characterConfig.name))
		{
			return null;
		}
		return ResourceManager.Instance.GetCharacterSprite(characterConfig.name);
	}

	public static AdobeAnimateSprite GetPacketSprite(TowerDefensePacketConfig config)
	{
		PackedScene packetSpriteScene = GetPacketSpriteScene(config);
		if (!GodotObject.IsInstanceValid(packetSpriteScene))
		{
			return null;
		}
		return packetSpriteScene.Instantiate(PackedScene.GenEditState.Disabled) as AdobeAnimateSprite;
	}

	public static PackedScene GetChacraterScene(string charcterName)
	{
		return ResourceManager.Instance.GetCharacterScene(charcterName);
	}

	public TowerDefenseCharacter CreateCharacter(string characterName, Vector2I gridPos = default(Vector2I))
	{
		if (gridPos == default(Vector2I))
		{
			gridPos = new Vector2I(-1, -1);
		}
		if (!ModLoader.TryInstantiateEffectiveCharacter(characterName, out var character, out var diagnostic))
		{
			if (!string.IsNullOrWhiteSpace(diagnostic))
			{
				GD.PushWarning("[TowerDefenseManager] Mod 角色脚本实例化失败，回退内置场景路径：" + characterName + ": " + diagnostic);
			}
			character = GetChacraterScene(characterName).Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
		}
		character.gridPos = gridPos;
		return character;
	}

	public long GetCoin()
	{
		if (_coinBank != null)
		{
			return _coinBank.num;
		}
		return 0L;
	}

	public void AddCoin(long num)
	{
		if (_coinBank != null)
		{
			_coinBank.AddNum(num);
		}
	}

	public void UseCoin(long num)
	{
		if (_coinBank != null)
		{
			_coinBank.UseCoin(num);
		}
	}

	public static CollectableConfig GetCollectable(string collectableName)
	{
		return (CollectableConfig)ResourceManager.Instance.COLLECTABLES[collectableName];
	}

	public override void _Ready()
	{
		Instance = this;
		eventBus = BattleEventBus.Instance;
		characterRegistry = new TowerDefenseBattleCharacterRegistry();
		AddChild(characterRegistry, forceReadableName: false, InternalMode.Disabled);
		targetSystem = new TargetSystem(characterRegistry, this);
		AddChild(targetSystem, forceReadableName: false, InternalMode.Disabled);
		damagePipeline = new DamagePipeline();
		BattleEventBus.Instance.OnCharacterDestroy += CharacterDestroy;
		_coinBank = GetNode<CoinBank>("%CoinBank");
		GameSaveManager.Instance?.SyncCoinBankFromSave();
	}

	public void MapIsChange()
	{
		gridSize = GetMapGridSize();
		gridBeginPos = GetMapGridBeginPos();
		gridNum = GetMapGridNum();
		ReevaluateCharacterSleepStates();
	}

	private void ReevaluateCharacterSleepStates()
	{
		if (!GodotObject.IsInstanceValid(characterRegistry))
		{
			return;
		}
		List<TowerDefenseCharacter> cleanCharactersList = characterRegistry.GetCleanCharactersList();
		for (int i = 0; i < cleanCharactersList.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = cleanCharactersList[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				(towerDefenseCharacter.componentManager?.GetRuntime<SleepComponent>("character.sleep"))?.ReevaluateEnvironmentState();
			}
		}
	}

	public void CharacterDestroy(TowerDefensePacketConfig _packet, Vector2 pos, Vector2 _gridPos, TowerDefenseEnum.CHARACTER_CAMP _camp, double _scale = 1.0, double _hitpointScale = 1.0)
	{
		if (GodotObject.IsInstanceValid(_packet) && _packet.characterConfig is TowerDefenseZombieConfig)
		{
			DeathHistory.Add(new TowerDefenseDeathRecord(_packet, pos, (Vector2I)_gridPos, _camp, _scale, _hitpointScale, invisible: false, _packet.saveKey != "ZombieAngel" && pos.X >= GetMapCellPos(new Vector2I(4, 0)).X));
		}
	}

	public static void ClearDeathRecords()
	{
		DeathHistory.Clear();
	}

	internal static bool HasDeathRecord(TowerDefenseEnum.CHARACTER_CAMP camp, bool requireAngelEligible = false)
	{
		return DeathHistory.HasDeathRecord(camp, requireAngelEligible);
	}

	internal static bool TryTakeLatestDeathRecord(TowerDefenseEnum.CHARACTER_CAMP camp, bool requireAngelEligible, out TowerDefenseDeathRecord record)
	{
		return DeathHistory.TryTakeLatest(camp, requireAngelEligible, out record);
	}

	public string GetEffectDirtName()
	{
		GeneralEnum.HOMEWORLD levelHomeworld = GetLevelHomeworld();
		Variant variant = ResourceManager.Instance.Get("effect_dirt_name");
		if (variant.VariantType == Variant.Type.Nil)
		{
			return "DirtSpawnDirt";
		}
		Dictionary dictionary = variant.AsGodotDictionary();
		int from = (int)levelHomeworld;
		if (!dictionary.ContainsKey(Variant.From(in from)))
		{
			return "DirtSpawnDirt";
		}
		from = (int)levelHomeworld;
		return dictionary[Variant.From(in from)].AsString();
	}

	public AdobeAnimateSprite GetEffectSprite(string effectSpriteName)
	{
		return ResourceManager.Instance.GetCharacterSprite(effectSpriteName).Instantiate(PackedScene.GenEditState.Disabled) as AdobeAnimateSprite;
	}

	public static TowerDefenseEffectParticlesOnce CreateEffectParticlesOnce(PackedScene scene, Vector2I gridPos = default(Vector2I))
	{
		if (gridPos == default(Vector2I))
		{
			gridPos = new Vector2I(-1, -1);
		}
		TowerDefenseEffectParticlesOnce obj = TOWER_DEFENSE_EFFECT_PARTICLES_ONCE.Instantiate(PackedScene.GenEditState.Disabled) as TowerDefenseEffectParticlesOnce;
		obj.Init(scene);
		obj.gridPos = gridPos;
		return obj;
	}

	public static TowerDefenseEffectSpriteOnce CreateEffectSpriteOnce(PackedScene scene, Vector2I gridPos = default(Vector2I), string clip = "")
	{
		if (gridPos == default(Vector2I))
		{
			gridPos = new Vector2I(-1, -1);
		}
		TowerDefenseEffectSpriteOnce obj = TOWER_DEFENSE_EFFECT_SPRITE_ONCE.Instantiate(PackedScene.GenEditState.Disabled) as TowerDefenseEffectSpriteOnce;
		obj.Init(scene, clip);
		obj.gridPos = gridPos;
		return obj;
	}

	public static bool TryCreateEffectSpriteOnceGpu(PackedScene scene, out TowerDefenseEffectSpriteOnceGpuHandle handle, Vector2I gridPos = default(Vector2I), string clips = "", Transform2D? transform = null, Action<string> onAnimeCompleted = null)
	{
		handle = default(TowerDefenseEffectSpriteOnceGpuHandle);
		if (scene == null)
		{
			return false;
		}
		if (gridPos == default(Vector2I))
		{
			gridPos = new Vector2I(-1, -1);
		}
		TowerDefenseEffectSpriteOnceBatcher orCreateNodeFreeFast = TowerDefenseEffectSpriteOnceBatcher.GetOrCreateNodeFreeFast();
		if (orCreateNodeFreeFast == null)
		{
			return false;
		}
		if (!transform.HasValue)
		{
			return orCreateNodeFreeFast.TryRegisterNodeFreeIdentity(scene, gridPos, clips, onAnimeCompleted, out handle);
		}
		return orCreateNodeFreeFast.TryRegisterNodeFree(scene, gridPos, clips, transform.Value, onAnimeCompleted, out handle);
	}

	public TowerDefenseEffectParticlesOnce CreateEffectParticlesSceneOnce(GPUParticles2DOnece scene, Vector2I gridPos = default(Vector2I))
	{
		if (gridPos == default(Vector2I))
		{
			gridPos = new Vector2I(-1, -1);
		}
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseEffectParticlesOnce.Create();
		towerDefenseEffectParticlesOnce.InitScene(scene);
		towerDefenseEffectParticlesOnce.gridPos = gridPos;
		return towerDefenseEffectParticlesOnce;
	}

	public TowerDefenseEffectSpriteOnce CreateEffectSpriteSceneOnce(AdobeAnimateSprite scene, Vector2I gridPos = default(Vector2I), string clip = "")
	{
		if (gridPos == default(Vector2I))
		{
			gridPos = new Vector2I(-1, -1);
		}
		TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseEffectSpriteOnce.Create();
		towerDefenseEffectSpriteOnce.InitScene(scene, clip);
		towerDefenseEffectSpriteOnce.gridPos = gridPos;
		return towerDefenseEffectSpriteOnce;
	}

	public static bool TryCreateEffectSceneOnceFast(PackedScene scene, Node2D parent, Vector2I gridPosition, Vector2 globalPosition, bool preferSprite)
	{
		if (scene == null || !GodotObject.IsInstanceValid(parent) || !globalPosition.IsFinite())
		{
			return false;
		}
		Transform2D value = new Transform2D(0f, globalPosition);
		TowerDefenseEffectSpriteOnceGpuHandle handle;
		TowerDefenseEffectParticlesOnce effect;
		if (preferSprite)
		{
			if (TryCreateEffectSpriteOnceGpu(scene, out handle, gridPosition, "", value))
			{
				return true;
			}
			return TowerDefenseEffectParticlesOnce.TrySpawnRetained(scene, parent, gridPosition, globalPosition, out effect);
		}
		if (TowerDefenseEffectParticlesOnce.TrySpawnRetained(scene, parent, gridPosition, globalPosition, out effect))
		{
			return true;
		}
		return TryCreateEffectSpriteOnceGpu(scene, out handle, gridPosition, "", value);
	}

	public TowerDefenseGroundItemBase FallingObjectCreate(Vector2 pos, double height = 0.0, Vector2 velocity = default(Vector2), double gravity = 0.0)
	{
		if (!TryPickZombieDeathFallingObject(out var id))
		{
			return null;
		}
		return FallingObjectItemCreate(id, pos, height, velocity, gravity);
	}

	public bool TryPickZombieDeathFallingObject(out ObjectManagerConfig.OBJECT id)
	{
		id = (GodotObject.IsInstanceValid(ZOMBIE_DEATH_FALLING_OBJECT) ? ZOMBIE_DEATH_FALLING_OBJECT.Pick() : ObjectManagerConfig.OBJECT.NOONE);
		return id != ObjectManagerConfig.OBJECT.NOONE;
	}

	public TowerDefenseGroundItemBase FallingObjectItemCreate(ObjectManagerConfig.OBJECT id, Vector2 pos, double height = 0.0, Vector2 velocity = default(Vector2), double gravity = 0.0)
	{
		if (id == ObjectManagerConfig.OBJECT.NOONE)
		{
			return null;
		}
		Node2D characterNode = GetCharacterNode();
		Node node = ObjectManager.PoolPop(id, characterNode);
		if (!(node is Node2D node2D))
		{
			if (GodotObject.IsInstanceValid(node))
			{
				ObjectManager.PoolPush(id, node);
			}
			return null;
		}
		if (!(node is TowerDefenseGroundItemBase towerDefenseGroundItemBase))
		{
			ObjectManager.PoolPush(id, node);
			return null;
		}
		node2D.GlobalPosition = pos;
		if (node is TowerDefenseCoinBase towerDefenseCoinBase)
		{
			DropItemConfig byId = DropItemRegistry.GetById(id);
			if (byId != null && byId.Value > 0)
			{
				towerDefenseCoinBase.num = byId.Value;
			}
			towerDefenseCoinBase.Init(height, velocity, gravity);
		}
		CaptureFallingObjectSpawn(towerDefenseGroundItemBase, id, pos, height, velocity, gravity);
		return towerDefenseGroundItemBase;
	}

	internal IDisposable BeginFallingObjectReplicationCapture()
	{
		return new FallingObjectReplicationCapture(this, _activeFallingObjectReplicationCapture);
	}

	private void CaptureFallingObjectSpawn(TowerDefenseGroundItemBase item, ObjectManagerConfig.OBJECT objectId, Vector2 position, double height, Vector2 velocity, double gravity)
	{
		_activeFallingObjectReplicationCapture?.Record(item, objectId, position, height, velocity, gravity);
	}

	private void FlushFallingObjectSpawns(List<FallingObjectSpawnRecord> records)
	{
		if (!Global.IsMultiplayerMode || !MultiPlayerManager.IsHost)
		{
			return;
		}
		foreach (FallingObjectSpawnRecord record in records)
		{
			if (GodotObject.IsInstanceValid(record.Item))
			{
				MultiPlayerManager.Instance?.SendSpawnFallingObject(record.ObjectId, record.Position.X, record.Position.Y, record.Velocity.X, record.Velocity.Y, record.Gravity, record.Height, record.Item.gridPos.X, record.Item.gridPos.Y);
			}
		}
	}

	public static TowerDefenseEnum.LEVEL_FINISH_METHOD GetGameMethod()
	{
		if (GodotObject.IsInstanceValid(Instance.currentControl))
		{
			if (Instance.currentControl.levelConfig is TowerDefenseLevelConfig towerDefenseLevelConfig)
			{
				return towerDefenseLevelConfig.finishMethod;
			}
			return TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE;
		}
		return TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE;
	}

	public bool IsIZMMode()
	{
		if (IsLevelEditorStage())
		{
			if (TryGetEditorFinishMethod(out var finishMethod))
			{
				return finishMethod == TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM;
			}
			return false;
		}
		TowerDefenseEnum.LEVEL_FINISH_METHOD gameMethod = GetGameMethod();
		if (gameMethod != TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM)
		{
			return gameMethod == TowerDefenseEnum.LEVEL_FINISH_METHOD.QUIZ;
		}
		return true;
	}

	public bool IsIZM2Mode()
	{
		if (IsLevelEditorStage())
		{
			if (TryGetEditorFinishMethod(out var finishMethod))
			{
				return finishMethod == TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM2;
			}
			return false;
		}
		return GetGameMethod() == TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM2;
	}

	private static bool IsLevelEditorStage()
	{
		if (Global.Instance != null && Global.Instance.isEditor)
		{
			return SceneManager.CurrentScene == "LevelEditorStage";
		}
		return false;
	}

	private static bool TryGetEditorFinishMethod(out TowerDefenseEnum.LEVEL_FINISH_METHOD finishMethod)
	{
		finishMethod = TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE;
		if (!GodotObject.IsInstanceValid(LevelEditorInformationEditor.Instance))
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(LevelEditorInformationEditor.Instance.levelConfig))
		{
			return false;
		}
		finishMethod = LevelEditorInformationEditor.Instance.levelConfig.finishMethod;
		return true;
	}

	public bool IsGameRunning()
	{
		if (GodotObject.IsInstanceValid(currentControl))
		{
			return currentControl.isGameRunning;
		}
		return false;
	}

	public static bool IsUnlimitedFire()
	{
		return CommandManager.Instance.debugUnlimitedFire;
	}

	public async void CoinCreate(Vector2 pos, int num, double height = 0.0, Vector2 velocity = default(Vector2), double gravity = 0.0, bool _collect = false)
	{
		Node2D characterNode = GetCharacterNode();
		while (num >= 1000)
		{
			TowerDefenseGroundItemBase towerDefenseGroundItemBase = Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_DIAMOND, pos, height, velocity, gravity);
			towerDefenseGroundItemBase.gridPos = new Vector2I(towerDefenseGroundItemBase.gridPos.X, 200);
			towerDefenseGroundItemBase.Reparent(characterNode, keepGlobalTransform: false);
			if (_collect)
			{
				towerDefenseGroundItemBase.Collection();
			}
			num -= 1000;
			await ToSignal(GetTree().CreateTimer(0.1, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		}
		while (num >= 50)
		{
			TowerDefenseGroundItemBase towerDefenseGroundItemBase2 = Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_GOLD, pos, height, velocity, gravity);
			towerDefenseGroundItemBase2.gridPos = new Vector2I(towerDefenseGroundItemBase2.gridPos.X, 200);
			towerDefenseGroundItemBase2.Reparent(characterNode, keepGlobalTransform: false);
			if (_collect)
			{
				towerDefenseGroundItemBase2.Collection();
			}
			num -= 50;
			await ToSignal(GetTree().CreateTimer(0.1, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		}
		while (num >= 10)
		{
			TowerDefenseGroundItemBase towerDefenseGroundItemBase3 = Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_SILVER, pos, height, velocity, gravity);
			towerDefenseGroundItemBase3.gridPos = new Vector2I(towerDefenseGroundItemBase3.gridPos.X, 200);
			towerDefenseGroundItemBase3.Reparent(characterNode, keepGlobalTransform: false);
			if (_collect)
			{
				towerDefenseGroundItemBase3.Collection();
			}
			num -= 10;
			await ToSignal(GetTree().CreateTimer(0.1, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		}
	}

	public void AddSun(long num)
	{
		TowerDefenseBattleFeatureSun sunFeature = GetSunFeature();
		if (sunFeature != null)
		{
			if (Global.Instance.isMultiplayerMode && num > 0)
			{
				num = (long)Math.Max(1.0, Math.Floor((double)num / 2.0));
			}
			sunFeature.AddSun(num);
		}
	}

	public void UseSun(long num)
	{
		GetSunFeature()?.UseSun(num);
	}

	public void SetSun(long num)
	{
		GetSunFeature()?.SetSun(num);
	}

	public long GetSun()
	{
		return GetSunFeature()?.sunNum ?? (-1);
	}

	public int GetLevelChapterFinishNum(string levelListName, string chapterName)
	{
		Dictionary dictionary = (Dictionary)ResourceManager.LEVEL_RESOURCE.Get("data");
		if (!dictionary.ContainsKey(levelListName))
		{
			return -1;
		}
		foreach (Variant item in (Godot.Collections.Array)dictionary[levelListName].AsGodotDictionary()["Chapter"])
		{
			Dictionary dictionary2 = item.AsGodotDictionary();
			if (dictionary2["Name"].AsString() != chapterName)
			{
				continue;
			}
			int num = 0;
			foreach (Variant item2 in (Godot.Collections.Array)dictionary2["Level"])
			{
				Dictionary dictionary3 = item2.AsGodotDictionary();
				if (GameSaveManager.Instance.GetLevelValue(dictionary3["SaveKey"].AsString()).GetValueOrDefault("Key", Variant.From<Dictionary>(new Dictionary())).AsGodotDictionary()
					.GetValueOrDefault("Finish", 0)
					.AsInt32() > 0)
				{
					num++;
				}
			}
			return num;
		}
		return -1;
	}

	public void ExecuteLevelEvent(IEnumerable<TowerDefenseLevelEventBase> eventList)
	{
		if (eventList == null)
		{
			return;
		}
		int num = 0;
		foreach (TowerDefenseLevelEventBase @event in eventList)
		{
			try
			{
				if (GodotObject.IsInstanceValid(@event))
				{
					@event.Execute();
				}
			}
			catch (Exception value)
			{
				GD.PushError($"[LevelEvent] Event {num} failed: {value}");
			}
			num++;
		}
	}

	public TowerDefenseLevelEventBase GetLevelEvent(string eventName)
	{
		if (string.IsNullOrWhiteSpace(eventName))
		{
			return null;
		}
		TowerDefenseLevelEventBase towerDefenseLevelEventBase = TowerDefenseLevelEventRegistry.Create(eventName);
		if (GodotObject.IsInstanceValid(towerDefenseLevelEventBase))
		{
			return towerDefenseLevelEventBase;
		}
		if (!GodotObject.IsInstanceValid(ResourceManager.Instance) || !ResourceManager.Instance.TOWERDEFENSE_LEVEL_EVENT.TryGetValue(eventName, out var value))
		{
			return null;
		}
		if (value is GDScript gDScript)
		{
			return gDScript.New().AsGodotObject() as TowerDefenseLevelEventBase;
		}
		if (value is TowerDefenseLevelEventBase towerDefenseLevelEventBase2)
		{
			return towerDefenseLevelEventBase2.Duplicate(deep: true) as TowerDefenseLevelEventBase;
		}
		return null;
	}

	public TowerDefenseLevelBaseConfig SetNextLevel(string levelChoose, int chapterId, int levelId)
	{
		if (Global.Instance.enterLevelMode == "ModLevel")
		{
			return SelectNextModLevel();
		}
		if (string.IsNullOrEmpty(levelChoose) || chapterId < 0 || levelId < 0)
		{
			return null;
		}
		if (!TryGetNextLevelData(levelChoose, chapterId, levelId, out var nextLevel))
		{
			return null;
		}
		if (!CanOpenNextLevel(nextLevel))
		{
			return null;
		}
		TowerDefenseLevelConfig towerDefenseLevelConfig = LoadNextLevelConfig(nextLevel);
		if (towerDefenseLevelConfig == null)
		{
			return null;
		}
		Instance.currentLevelConfig = towerDefenseLevelConfig;
		SaveNextLevelProgress(chapterId, levelId);
		return Instance.currentLevelConfig as TowerDefenseLevelConfig;
	}

	private bool TryGetNextLevelData(string levelChoose, int chapterId, int levelId, out Dictionary nextLevel)
	{
		nextLevel = null;
		Dictionary dictionary = (Dictionary)ResourceManager.LEVEL_RESOURCE.Get("data");
		if (!dictionary.ContainsKey(levelChoose))
		{
			return false;
		}
		Godot.Collections.Array array = (Godot.Collections.Array)dictionary[levelChoose].AsGodotDictionary()["Chapter"];
		if (chapterId >= array.Count)
		{
			return false;
		}
		Godot.Collections.Array array2 = (Godot.Collections.Array)array[chapterId].AsGodotDictionary()["Level"];
		if (levelId + 1 >= array2.Count)
		{
			return false;
		}
		nextLevel = array2[levelId + 1].AsGodotDictionary();
		return true;
	}

	private TowerDefenseLevelConfig LoadNextLevelConfig(Dictionary nextLevel)
	{
		string text = GameSaveManager.Instance.GetKeyValue("CurrentDifficult").AsString();
		Dictionary dictionary = nextLevel["Level"].AsGodotDictionary();
		if (dictionary.ContainsKey(text) && dictionary[text].AsString() != "")
		{
			return GD.Load<TowerDefenseLevelConfig>(dictionary[text].AsString());
		}
		if (dictionary.ContainsKey("Normal") && dictionary["Normal"].AsString() != "")
		{
			return GD.Load<TowerDefenseLevelConfig>(dictionary["Normal"].AsString());
		}
		return null;
	}

	private bool CanOpenNextLevel(Dictionary nextLevel)
	{
		if (nextLevel["OpenKey"].AsString() == "Lock")
		{
			return false;
		}
		if (nextLevel["OpenKey"].AsString() != "" && GameSaveManager.Instance.GetLevelValue(nextLevel["OpenKey"].AsString()).GetValueOrDefault("Key", Variant.From<Dictionary>(new Dictionary())).AsGodotDictionary()
			.GetValueOrDefault("Finish", 0)
			.AsInt32() <= 0)
		{
			return false;
		}
		return true;
	}

	private void SaveNextLevelProgress(int chapterId, int levelId)
	{
		Global.Instance.currentLevelId = levelId + 1;
		GameSaveManager.Instance.SetKeyValue($"AdventureChapter{chapterId + 1}Index", levelId + 1);
		GameSaveManager.Instance.Save();
	}

	public TowerDefenseInGameLevelControl GetLevelControl()
	{
		if (!GodotObject.IsInstanceValid(currentControl))
		{
			return null;
		}
		return currentControl.levelControl;
	}

	public void TipsPlay(string text, double duration = 2.0)
	{
		if (Global.Instance.isMultiplayerMode && GodotObject.IsInstanceValid(currentControl))
		{
			currentControl.TipsPlay(text, duration);
		}
		else
		{
			GetLevelControl().TipsPlay(text, duration);
		}
	}

	public GeneralEnum.HOMEWORLD GetLevelHomeworld()
	{
		return GetLevelControl()?.config.homeWorld ?? GeneralEnum.HOMEWORLD.NOONE;
	}

	public TowerDefenseSunBase MagicSunCreate(Vector2 pos, long sunNum, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.LAND, double height = 0.0, Vector2 velocity = default(Vector2), double gravity = 980.0, double moveStopTime = -1.0)
	{
		return CreateSunDrop(ObjectManagerConfig.OBJECT.SUN_MAGIC, EconomyAccountId.Local, SunDropOwnershipPolicy.LegacySharedReplica, pos, sunNum, movingMethod, height, velocity, gravity, moveStopTime);
	}

	public TowerDefenseSunBase MagicSunCreate(EconomyAccountId accountId, Vector2 pos, long sunNum, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.LAND, double height = 0.0, Vector2 velocity = default(Vector2), double gravity = 980.0, double moveStopTime = -1.0)
	{
		if (!CanCreateAccountOwnedSunDrop(accountId))
		{
			return null;
		}
		return CreateSunDrop(ObjectManagerConfig.OBJECT.SUN_MAGIC, accountId, SunDropOwnershipPolicy.AccountOwned, pos, sunNum, movingMethod, height, velocity, gravity, moveStopTime);
	}

	public static double GetMapAttackDpsLifestealRatio()
	{
		TowerDefenseMapConfig activeMapConfig = GetActiveMapConfig();
		if (GodotObject.IsInstanceValid(activeMapConfig))
		{
			return activeMapConfig.GetAttackDpsLifestealRatio();
		}
		return 0.0;
	}

	public static double ApplyMapAttackDpsLifesteal(TowerDefenseCharacter attacker, double damage)
	{
		if (!GodotObject.IsInstanceValid(attacker?.instance) || !double.IsFinite(damage) || damage <= 0.0)
		{
			return 0.0;
		}
		double num = damage * GetMapAttackDpsLifestealRatio();
		if (!double.IsFinite(num) || num <= 0.0)
		{
			return 0.0;
		}
		attacker.instance.hitpoints += num;
		ShowHealthComponent showHealthComponent = attacker.showHealthComponent;
		if (showHealthComponent != null && !showHealthComponent.IsReleased)
		{
			attacker.showHealthComponent.MarkDirty();
		}
		return num;
	}

	public static void ApplyMapCharacterRules(TowerDefenseCharacter character)
	{
		TowerDefenseMapConfig activeMapConfig = GetActiveMapConfig();
		if (GodotObject.IsInstanceValid(activeMapConfig))
		{
			activeMapConfig.ApplyCharacterRules(character);
		}
	}

	public static double GetMapZombieColumnSpeedMultiplier(int gridX)
	{
		TowerDefenseMapConfig activeMapConfig = GetActiveMapConfig();
		if (GodotObject.IsInstanceValid(activeMapConfig))
		{
			return activeMapConfig.GetZombieColumnSpeedMultiplier(gridX);
		}
		return 1.0;
	}

	public TowerDefenseMap GetCurrentMap()
	{
		return GetMapCurrentMap();
	}

	public TowerDefenseMapConfig GetMapConfig(string mapName)
	{
		TowerDefenseMapConfig result = null;
		if (!string.IsNullOrEmpty(mapName) && ResourceManager.Instance.MAPS.ContainsKey(mapName))
		{
			result = (TowerDefenseMapConfig)ResourceManager.Instance.MAPS[mapName];
		}
		return result;
	}

	public void MapChange(string map, double duration = 0.0, double delay = 0.0)
	{
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature))
		{
			TowerDefenseMapConfig mapConfig = GetMapConfig(map);
			if (GodotObject.IsInstanceValid(mapConfig))
			{
				mapFeature.MapChange(mapConfig, duration, delay);
			}
		}
	}

	public static bool GetMapIsNight()
	{
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature) && GodotObject.IsInstanceValid(mapFeature.config))
		{
			return mapFeature.config.isNight;
		}
		return false;
	}

	public void MapDayNightSwitch(double duration = 2.0, double _switchTimer = 100.0, double returnDuration = 2.0)
	{
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature))
		{
			mapFeature.MapDayNightSwitch(duration, _switchTimer, returnDuration);
		}
	}

	public Vector2I GetMapGridNum()
	{
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature) && GodotObject.IsInstanceValid(mapFeature.config))
		{
			return mapFeature.config.gridNum;
		}
		return new Vector2I(25, 25);
	}

	public Vector2 GetMapGridSize()
	{
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature) && GodotObject.IsInstanceValid(mapFeature.config) && GodotObject.IsInstanceValid(mapFeature.mapControl))
		{
			return mapFeature.config.gridSize * mapFeature.mapControl.GlobalScale;
		}
		return new Vector2(80f, 98f);
	}

	public Vector2 GetMapGridBeginPos()
	{
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature) && GodotObject.IsInstanceValid(mapFeature.config) && GodotObject.IsInstanceValid(mapFeature.mapControl))
		{
			return mapFeature.config.gridBeginPos * mapFeature.mapControl.GlobalScale + mapFeature.mapControl.GlobalPosition;
		}
		return Vector2.Zero;
	}

	public double GetMapPlantOffset()
	{
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature) && GodotObject.IsInstanceValid(mapFeature.config))
		{
			return mapFeature.config.plantOffset;
		}
		return 50.0;
	}

	public Vector2I GetMapGridPos(Vector2 pos)
	{
		Vector2 vector = ((pos - gridBeginPos) / gridSize).Floor();
		return new Vector2I((int)vector.X, (int)vector.Y) + Vector2I.One;
	}

	public Vector2 GetMapCellPos(Vector2I gridPos)
	{
		gridPos -= Vector2I.One;
		return gridBeginPos + new Vector2(gridPos.X, gridPos.Y) * gridSize;
	}

	public Vector2 GetMapCellPosCenter(Vector2I gridPos)
	{
		gridPos -= Vector2I.One;
		return gridBeginPos + new Vector2(gridPos.X, gridPos.Y) * gridSize + GetMapGridSize() / 2f;
	}

	public bool CheckMapGridPosIn(Vector2I gridPos)
	{
		if (gridPos.X < 1 || gridPos.Y < 1 || gridPos.X > gridNum.X || gridPos.Y > gridNum.Y)
		{
			return false;
		}
		return true;
	}

	public static TowerDefenseCellInstance GetMapCell(Vector2I gridPos)
	{
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (!GodotObject.IsInstanceValid(mapFeature))
		{
			return null;
		}
		return mapFeature.GetPlantGridCell(gridPos);
	}

	public void SetMapGridType(TowerDefenseCellConfig cellConfig)
	{
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature))
		{
			mapFeature.SetGridType(cellConfig);
		}
	}

	public void SetMapLineUse(int line, bool use)
	{
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature))
		{
			mapFeature.SetLineUse(line, use);
		}
	}

	public bool GetMapLineUse(int line)
	{
		Array<bool> mapLineUseArr = GetMapLineUseArr();
		if (line > 0 && line < mapLineUseArr.Count)
		{
			return mapLineUseArr[line];
		}
		return false;
	}

	public double GetMapGroundLeft()
	{
		return gridBeginPos.X;
	}

	public double GetMapGroundRight()
	{
		return gridBeginPos.X + (float)gridNum.X * gridSize.X;
	}

	public double GetMapGroundUp()
	{
		return gridBeginPos.Y;
	}

	public double GetMapGroundDown()
	{
		return gridBeginPos.Y + (float)gridNum.Y * gridSize.Y;
	}

	public Vector2I GetMapGridPosFromMouse(Vector2 pos)
	{
		int num = (int)Mathf.Floor((pos.X - gridBeginPos.X) / gridSize.X);
		double num2 = (pos.X - (gridBeginPos.X + (float)num * gridSize.X)) / gridSize.X;
		Array<Godot.Collections.Array> mapPlantGrid = GetMapPlantGrid();
		if (mapPlantGrid.Count == 0)
		{
			return new Vector2I(num, -1);
		}
		for (int i = 0; i < gridNum.Y; i++)
		{
			Vector2I result = new Vector2I(num, i) + Vector2I.One;
			if (result.X < 1 || result.Y < 1 || result.X > gridNum.X || result.Y > gridNum.Y || mapPlantGrid.Count <= result.X)
			{
				continue;
			}
			Variant variant = mapPlantGrid[result.X][result.Y];
			if (variant.VariantType == Variant.Type.Nil)
			{
				continue;
			}
			TowerDefenseCellInstance towerDefenseCellInstance = variant.AsGodotObject() as TowerDefenseCellInstance;
			if (!GodotObject.IsInstanceValid(towerDefenseCellInstance))
			{
				continue;
			}
			int num3 = 0;
			CurveTexture groundHeightCurve = towerDefenseCellInstance.groundHeightCurve;
			if (GodotObject.IsInstanceValid(groundHeightCurve))
			{
				Curve curve = groundHeightCurve.Curve;
				if (curve != null)
				{
					num3 = (int)curve.Sample((float)num2);
				}
			}
			double num4 = gridBeginPos.Y + (float)i * gridSize.Y;
			if ((double)pos.Y > num4 - (double)num3 && (double)pos.Y < num4 - (double)num3 + (double)gridSize.Y)
			{
				return result;
			}
		}
		return new Vector2I(num, -1);
	}

	public TowerDefenseMapControl GetMapControl()
	{
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature))
		{
			return mapFeature.mapControl;
		}
		return null;
	}

	public void ReleaseBattleReferences(TowerDefenseControlNew control, bool preserveLevelConfig)
	{
		if (currentControl == control)
		{
			currentControl = null;
			if (!preserveLevelConfig)
			{
				currentLevelConfig = null;
			}
			_groupCache.Clear();
			_groupCacheFrame = -1L;
		}
		if (_cachedMapFeatureControl == control)
		{
			_cachedMapFeature = null;
			_cachedMapFeatureControl = null;
		}
	}

	public static TowerDefenseBattleFeatureMap GetMapFeature()
	{
		if (Instance == null)
		{
			return null;
		}
		TowerDefenseControlNew towerDefenseControlNew = Instance.currentControl;
		if (GodotObject.IsInstanceValid(towerDefenseControlNew))
		{
			if (_cachedMapFeature != null && _cachedMapFeatureControl == towerDefenseControlNew)
			{
				return _cachedMapFeature;
			}
			_cachedMapFeatureControl = towerDefenseControlNew;
			_cachedMapFeature = towerDefenseControlNew.GetFeature(FeatureName_Map) as TowerDefenseBattleFeatureMap;
			return _cachedMapFeature;
		}
		_cachedMapFeature = null;
		_cachedMapFeatureControl = null;
		if (GodotObject.IsInstanceValid(LevelEditorMapEditor.instance) && GodotObject.IsInstanceValid(LevelEditorMapEditor.instance.mapFeature))
		{
			return LevelEditorMapEditor.instance.mapFeature;
		}
		return null;
	}

	public static double ApplyMapPacketCooldownRules(TowerDefenseEnum.PACKET_TYPE packetType, double multiplier)
	{
		TowerDefenseMapConfig activeMapConfig = GetActiveMapConfig();
		if (GodotObject.IsInstanceValid(activeMapConfig))
		{
			return activeMapConfig.ApplyPacketCooldownRules(packetType, multiplier);
		}
		return multiplier;
	}

	public static bool MapIgnoresDynamicPacketCostGrowth(TowerDefenseEnum.PACKET_TYPE packetType)
	{
		TowerDefenseMapConfig activeMapConfig = GetActiveMapConfig();
		if (GodotObject.IsInstanceValid(activeMapConfig))
		{
			return activeMapConfig.IgnoresDynamicPacketCostGrowth(packetType);
		}
		return false;
	}

	public static Vector2 GetMapCellPlantPos(Vector2I gridPos)
	{
		if (Instance == null)
		{
			return Vector2.Zero;
		}
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (mapFeature == null || !GodotObject.IsInstanceValid(mapFeature.mapControl))
		{
			return Vector2.Zero;
		}
		gridPos -= Vector2I.One;
		return Instance.gridBeginPos + new Vector2(gridPos.X, gridPos.Y) * Instance.gridSize + new Vector2(Instance.gridSize.X / 2f, (float)Instance.GetMapPlantOffset() * mapFeature.mapControl.GlobalScale.Y);
	}

	public static double GetMapLineY(int line)
	{
		if (Instance == null)
		{
			return 0.0;
		}
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (mapFeature == null || !GodotObject.IsInstanceValid(mapFeature.mapControl))
		{
			return 0.0;
		}
		line--;
		return Instance.gridBeginPos.Y + (float)line * Instance.gridSize.Y + (float)Instance.GetMapPlantOffset() * mapFeature.mapControl.GlobalScale.Y;
	}

	public TowerDefenseMapConfig GetCurrentMapConfig()
	{
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature))
		{
			return mapFeature.config;
		}
		return null;
	}

	public TowerDefenseMap GetMapCurrentMap()
	{
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature))
		{
			return mapFeature.currentMap;
		}
		return null;
	}

	public PacketPickControl GetPacketPickControl()
	{
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature))
		{
			return mapFeature.packetPickControl;
		}
		return null;
	}

	public static bool MapLineHasType(int line, TowerDefenseEnum.PLANTGRIDTYPE type)
	{
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature))
		{
			return mapFeature.LineHasType(line, type);
		}
		return false;
	}

	public Array<Godot.Collections.Array> GetMapPlantGrid()
	{
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature))
		{
			return mapFeature.plantGrid;
		}
		return new Array<Godot.Collections.Array>();
	}

	public Array<bool> GetMapLineUseArr()
	{
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature))
		{
			return mapFeature.lineUse;
		}
		return new Array<bool>();
	}

	public Godot.Collections.Array GetMapIceCapList()
	{
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature))
		{
			return mapFeature.iceCapList;
		}
		return new Godot.Collections.Array();
	}

	public Rect2 GetGroundRect()
	{
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature))
		{
			return mapFeature.groundRect;
		}
		return default(Rect2);
	}

	public void SetIceCapPos(int line, Vector2 pos)
	{
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature))
		{
			mapFeature.SetIceCapPos(line, pos);
		}
	}

	public bool TryGetIceCapFrontX(int line, out float frontX)
	{
		frontX = 0f;
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature))
		{
			return mapFeature.TryGetIceCapFrontX(line, out frontX);
		}
		return false;
	}

	public static bool MapHasSpecialRule(StringName ruleId)
	{
		TowerDefenseMapConfig activeMapConfig = GetActiveMapConfig();
		if (GodotObject.IsInstanceValid(activeMapConfig))
		{
			return activeMapConfig.HasSpecialRule(ruleId);
		}
		return false;
	}

	public static bool MapSpecialRulesPreventSleep()
	{
		TowerDefenseMapConfig activeMapConfig = GetActiveMapConfig();
		if (GodotObject.IsInstanceValid(activeMapConfig))
		{
			return activeMapConfig.SpecialRulesPreventSleep();
		}
		return false;
	}

	private static TowerDefenseMapConfig GetActiveMapConfig()
	{
		TowerDefenseBattleFeatureMap mapFeature = GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature) && GodotObject.IsInstanceValid(mapFeature.config))
		{
			return mapFeature.config;
		}
		return null;
	}

	private TowerDefenseLevelBaseConfig SelectNextModLevel()
	{
		XWModLevelIdentity identity = XWModLevelSession.Current;
		if (identity == null)
		{
			return null;
		}
		XWModContentCatalog.Catalog catalog = XWModContentCatalog.GetCatalogs().FirstOrDefault((XWModContentCatalog.Catalog item) => string.Equals(item.OwnerModId, identity.OwnerModId, StringComparison.OrdinalIgnoreCase) && item.Key == identity.CatalogKey);
		if (catalog == null)
		{
			return null;
		}
		bool flag = false;
		Godot.Collections.Array array = catalog.Data["Chapter"].AsGodotArray();
		for (int num = 0; num < array.Count; num++)
		{
			Dictionary dictionary = array[num].AsGodotDictionary();
			Godot.Collections.Array array2 = dictionary["Level"].AsGodotArray();
			for (int num2 = 0; num2 < array2.Count; num2++)
			{
				Dictionary dictionary2 = array2[num2].AsGodotDictionary();
				string text = dictionary2["SaveKey"].AsString();
				if (!flag)
				{
					flag = text == identity.LevelSaveKey;
					continue;
				}
				if (dictionary.GetValueOrDefault("Lock", false).AsBool() || !ModOpen(dictionary.GetValueOrDefault("OpenKey", "").AsString(), identity) || !ModOpen(dictionary2.GetValueOrDefault("OpenKey", "").AsString(), identity))
				{
					return null;
				}
				Dictionary dictionary3 = dictionary2["Level"].AsGodotDictionary();
				XWModLevelIdentity identity2 = identity with
				{
					LevelSaveKey = text,
					Difficulty = ((dictionary3.GetValueOrDefault(identity.Difficulty, "").AsString().Length > 0) ? identity.Difficulty : "Normal")
				};
				if (!XWModContentCatalog.TryResolve(identity2, out var config, out var _))
				{
					return null;
				}
				currentLevelConfig = config;
				XWModLevelSession.Select(identity2);
				Global.Instance.currentChapterId = num;
				Global.Instance.currentLevelId = num2;
				return config;
			}
		}
		return null;
	}

	private static bool ModOpen(string key, XWModLevelIdentity identity)
	{
		if (key.Length != 0)
		{
			if (key != "Lock")
			{
				return XWModPlayerProgressService.FinishCount(identity with
				{
					LevelSaveKey = key
				}) > 0;
			}
			return false;
		}
		return true;
	}

	public static MowerConfig GetMowerConfig(string mowerName)
	{
		return (MowerConfig)ResourceManager.Instance.MOWERS[mowerName];
	}

	public static Godot.Collections.Array GetMowerList()
	{
		List<Variant> list = new List<Variant>();
		foreach (string key in ResourceManager.Instance.MOWERS.Keys)
		{
			list.Add(key);
		}
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (Variant item in list)
		{
			array.Add(item);
		}
		return array;
	}

	public int GetMowerNum()
	{
		return GetMower().Count;
	}

	public Godot.Collections.Array GetMower()
	{
		return _GetNodesInGroupCached("Mower");
	}

	public TowerDefenseMower CreateMower(int line)
	{
		return GetMowerFeature()?.CreateMower(line);
	}

	public TowerDefenseBattleFeatureMower GetMowerFeature()
	{
		TowerDefenseControlNew towerDefenseControlNew = currentControl;
		if (towerDefenseControlNew != null)
		{
			return towerDefenseControlNew.GetFeature(FeatureName_Mower) as TowerDefenseBattleFeatureMower;
		}
		return null;
	}

	public TowerDefenseMowerManager GetMowerManager()
	{
		return GetMowerFeature()?.mowerManager;
	}

	public static NpcTalkConfig GetNpcTalk(string npcTalkName)
	{
		if (string.IsNullOrEmpty(npcTalkName))
		{
			return null;
		}
		if (ResourceManager.Instance.TALKS.TryGetValue(npcTalkName, out var value) && value is NpcTalkConfig result)
		{
			return result;
		}
		GD.PushWarning("[NpcTalk] Talk '" + npcTalkName + "' is unavailable.");
		return null;
	}

	public TowerDefenseInGamePacketBank GetPacketBank()
	{
		return GetPacketBankFeature()?.packetBank;
	}

	public TowerDefenseBattleFeaturePacketBank GetPacketBankFeature()
	{
		TowerDefenseControlNew towerDefenseControlNew = currentControl;
		if (towerDefenseControlNew != null)
		{
			return towerDefenseControlNew.GetFeature(FeatureName_PacketBank) as TowerDefenseBattleFeaturePacketBank;
		}
		return null;
	}

	public static TowerDefensePacketBankData GetPacketBankData(string packetBank)
	{
		ZLoggerErrorInterpolatedStringHandler message;
		bool enabled;
		if (ResourceManager.Instance == null)
		{
			ILogger logger = _logger;
			ILogger logger2 = logger;
			message = new ZLoggerErrorInterpolatedStringHandler(51, 0, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("GetPacketBankData: ResourceManager.Instance is null");
			}
			logger2.ZLogError(ref message);
			return null;
		}
		ResourceManager.Instance.RequireFullGameplayResourcesReady("GetPacketBankData(" + packetBank + ")");
		if (string.IsNullOrEmpty(packetBank) || !ResourceManager.Instance.TOWERDEFENSE_PACKETBANKS.ContainsKey(packetBank))
		{
			ILogger logger = _logger;
			ILogger logger3 = logger;
			message = new ZLoggerErrorInterpolatedStringHandler(66, 2, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("GetPacketBankData: '");
				message.AppendFormatted(packetBank, 0, null, "packetBank");
				message.AppendLiteral("' not found in TOWERDEFENSE_PACKETBANKS, keys=");
				message.AppendFormatted(string.Join(",", ResourceManager.Instance.TOWERDEFENSE_PACKETBANKS.Keys), 0, null, "string.Join(\",\", ResourceManager.Instance.TOWERDEFENSE_PACKETBANKS.Keys)");
			}
			logger3.ZLogError(ref message);
			return null;
		}
		TowerDefensePacketBankData towerDefensePacketBankData = ResourceManager.Instance.TOWERDEFENSE_PACKETBANKS[packetBank];
		if (towerDefensePacketBankData == null || !GodotObject.IsInstanceValid(towerDefensePacketBankData))
		{
			ILogger logger = _logger;
			ILogger logger4 = logger;
			message = new ZLoggerErrorInterpolatedStringHandler(43, 1, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("GetPacketBankData: '");
				message.AppendFormatted(packetBank, 0, null, "packetBank");
				message.AppendLiteral("' value is null/invalid");
			}
			logger4.ZLogError(ref message);
			return null;
		}
		return towerDefensePacketBankData;
	}

	public static TowerDefensePacketConfig GetPacketConfigReadOnlyByCharacterName(string characterName)
	{
		if (ResourceManager.Instance == null || characterName == "")
		{
			return null;
		}
		ResourceManager.Instance.RequireFullGameplayResourcesReady("GetPacketConfigReadOnlyByCharacterName");
		foreach (string packetName in ResourceManager.Instance.GetPacketNames())
		{
			TowerDefensePacketConfig packetConfigReadOnly = GetPacketConfigReadOnly(packetName);
			if (GodotObject.IsInstanceValid(packetConfigReadOnly) && GodotObject.IsInstanceValid(packetConfigReadOnly.characterConfig) && packetConfigReadOnly.characterConfig.name == characterName)
			{
				return packetConfigReadOnly;
			}
		}
		return null;
	}

	public static Array<TowerDefensePacketConfig> GetPacketConfigCostLower(int cost, TowerDefenseEnum.PACKET_TYPE type)
	{
		return CollectPacketConfigsByCost((TowerDefensePacketConfig packetConfig) => packetConfig.GetCost() <= cost && (packetConfig._GetType() == type || type == TowerDefenseEnum.PACKET_TYPE.NOONE));
	}

	public static Array<TowerDefensePacketConfig> GetPacketConfigCostLowerWithTypeList(int cost, Array<TowerDefenseEnum.PACKET_TYPE> typeList)
	{
		return CollectPacketConfigsByCost((TowerDefensePacketConfig packetConfig) => packetConfig.GetCost() <= cost && typeList.Contains(packetConfig._GetType()));
	}

	private static Array<TowerDefensePacketConfig> CollectPacketConfigsByCost(Func<TowerDefensePacketConfig, bool> predicate)
	{
		List<TowerDefensePacketConfig> list = new List<TowerDefensePacketConfig>();
		ResourceManager.Instance.RequireFullGameplayResourcesReady("CollectPacketConfigsByCost");
		foreach (KeyValuePair<string, Resource> tOWERDEFENSE_PACKET in ResourceManager.Instance.TOWERDEFENSE_PACKETS)
		{
			if (tOWERDEFENSE_PACKET.Value is TowerDefensePacketConfig towerDefensePacketConfig)
			{
				string key = tOWERDEFENSE_PACKET.Key;
				_packetConfigRefCache.TryAdd(key, towerDefensePacketConfig);
				if ((towerDefensePacketConfig.characterConfig is TowerDefensePlantConfig || towerDefensePacketConfig.characterConfig is TowerDefenseZombieConfig) && predicate(towerDefensePacketConfig))
				{
					list.Add((TowerDefensePacketConfig)towerDefensePacketConfig.Duplicate(deep: true));
				}
			}
		}
		Array<TowerDefensePacketConfig> array = new Array<TowerDefensePacketConfig>();
		foreach (TowerDefensePacketConfig item in list)
		{
			array.Add(item);
		}
		return array;
	}

	public static Array<TowerDefensePacketConfig> GetPacketConfigCostUpper(int cost, TowerDefenseEnum.PACKET_TYPE type)
	{
		return CollectPacketConfigsByCost((TowerDefensePacketConfig packetConfig) => packetConfig.GetCost() >= cost && packetConfig._GetType() == type);
	}

	public static Array<TowerDefensePacketConfig> GetPacketConfigCostUpperWithTypeList(int cost, Array<TowerDefenseEnum.PACKET_TYPE> typeList)
	{
		return CollectPacketConfigsByCost((TowerDefensePacketConfig packetConfig) => packetConfig.GetCost() >= cost && typeList.Contains(packetConfig._GetType()));
	}

	public static TowerDefensePacketConfig GetPacketConfig(string packetName)
	{
		ZLoggerErrorInterpolatedStringHandler message;
		bool enabled;
		if (ResourceManager.Instance == null)
		{
			ILogger logger = _logger;
			ILogger logger2 = logger;
			message = new ZLoggerErrorInterpolatedStringHandler(49, 0, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("GetPacketConfig: ResourceManager.Instance is null");
			}
			logger2.ZLogError(ref message);
			return null;
		}
		Variant packet = ResourceManager.Instance.GetPacket(packetName);
		if (packet.VariantType == Variant.Type.Nil)
		{
			ILogger logger = _logger;
			ILogger logger3 = logger;
			message = new ZLoggerErrorInterpolatedStringHandler(50, 1, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("GetPacketConfig: packet '");
				message.AppendFormatted(packetName, 0, null, "packetName");
				message.AppendLiteral("' not found (Nil variant)");
			}
			logger3.ZLogError(ref message);
			return null;
		}
		GodotObject godotObject = packet.AsGodotObject();
		if (godotObject == null || !GodotObject.IsInstanceValid(godotObject))
		{
			ILogger logger = _logger;
			ILogger logger4 = logger;
			message = new ZLoggerErrorInterpolatedStringHandler(52, 1, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("GetPacketConfig: packet '");
				message.AppendFormatted(packetName, 0, null, "packetName");
				message.AppendLiteral("' object is null or invalid");
			}
			logger4.ZLogError(ref message);
			return null;
		}
		if (!(godotObject is Resource resource))
		{
			ILogger logger = _logger;
			ILogger logger5 = logger;
			message = new ZLoggerErrorInterpolatedStringHandler(51, 2, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("GetPacketConfig: packet '");
				message.AppendFormatted(packetName, 0, null, "packetName");
				message.AppendLiteral("' is not a Resource, type=");
				message.AppendFormatted(godotObject.GetType().Name, 0, null, "obj.GetType().Name");
			}
			logger5.ZLogError(ref message);
			return null;
		}
		_packetConfigRefCache.TryAdd(packetName, resource as TowerDefensePacketConfig);
		Resource resource2 = resource.Duplicate(deep: true);
		if (resource2 == null)
		{
			ILogger logger = _logger;
			ILogger logger6 = logger;
			message = new ZLoggerErrorInterpolatedStringHandler(50, 1, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("GetPacketConfig: packet '");
				message.AppendFormatted(packetName, 0, null, "packetName");
				message.AppendLiteral("' Duplicate returned null");
			}
			logger6.ZLogError(ref message);
			return null;
		}
		return resource2 as TowerDefensePacketConfig;
	}

	public static TowerDefensePacketConfig GetPacketConfigReadOnly(string packetName)
	{
		Variant packet = ResourceManager.Instance.GetPacket(packetName);
		if (packet.VariantType == Variant.Type.Nil)
		{
			return null;
		}
		return (TowerDefensePacketConfig)packet.AsGodotObject();
	}

	public static TowerDefenseInGamePacketShow CreatePacketShow(string packetName = "")
	{
		TowerDefensePacketConfig config = null;
		if (packetName != "")
		{
			config = GetPacketConfig(packetName);
		}
		return CreatePacketShowWithConfig(config);
	}

	public static TowerDefenseInGamePacketShow CreatePacketShowWithConfig(TowerDefensePacketConfig config = null)
	{
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = TOWER_DEFENSE_IN_GAME_PACKET_SHOW.Instantiate(PackedScene.GenEditState.Disabled) as TowerDefenseInGamePacketShow;
		if (GodotObject.IsInstanceValid(config))
		{
			towerDefenseInGamePacketShow.Init(config);
		}
		return towerDefenseInGamePacketShow;
	}

	public TowerDefenseInGamePacketShow SpawnPacket(TowerDefensePacketConfig packetConfig, Vector2 pos, double aliveTime, bool isFall, bool useCost = false, bool useRandf = true, Vector2? velocityOverride = null)
	{
		return SpawnPacketCore(default(EconomyAccountId), packetConfig, pos, aliveTime, isFall, useCost, useRandf, velocityOverride);
	}

	public TowerDefenseInGamePacketShow SpawnPacket(EconomyAccountId accountId, TowerDefensePacketConfig packetConfig, Vector2 pos, double aliveTime, bool isFall, bool useCost = false, bool useRandf = true, Vector2? velocityOverride = null)
	{
		if (!accountId.IsValid)
		{
			return null;
		}
		return SpawnPacketCore(accountId, packetConfig, pos, aliveTime, isFall, useCost, useRandf, velocityOverride);
	}

	private TowerDefenseInGamePacketShow SpawnPacketCore(EconomyAccountId accountId, TowerDefensePacketConfig packetConfig, Vector2 pos, double aliveTime, bool isFall, bool useCost, bool useRandf, Vector2? velocityOverride)
	{
		if (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			return null;
		}
		double height = GD.RandRange(Instance.GetMapGridBeginPos().Y + 200f, Instance.GetMapGroundDown() - (double)Instance.GetMapGridBeginPos().Y);
		Node2D characterNode = GetCharacterNode();
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = CreatePacketShow();
		if (accountId.IsValid && !towerDefenseInGamePacketShow.TryBindSunAccount(accountId))
		{
			towerDefenseInGamePacketShow.QueueFree();
			return null;
		}
		towerDefenseInGamePacketShow.GlobalPosition = pos;
		characterNode.AddChild(towerDefenseInGamePacketShow, forceReadableName: false, InternalMode.Disabled);
		towerDefenseInGamePacketShow.Init(packetConfig);
		towerDefenseInGamePacketShow.onlyDraw = false;
		towerDefenseInGamePacketShow.showCost = useCost;
		towerDefenseInGamePacketShow.useCost = useCost;
		towerDefenseInGamePacketShow.plantOnce = true;
		towerDefenseInGamePacketShow.StartInit();
		towerDefenseInGamePacketShow.alive = true;
		towerDefenseInGamePacketShow.aliveTime = aliveTime;
		towerDefenseInGamePacketShow.ZIndex = 1024;
		if (useCost)
		{
			towerDefenseInGamePacketShow.start = true;
		}
		InitializeSpawnedPacketMovement(towerDefenseInGamePacketShow, isFall, height, useRandf, velocityOverride, out var velocityX, out var velocityY);
		BindSpawnedPacketPicker(towerDefenseInGamePacketShow);
		PublishSpawnedPacket(towerDefenseInGamePacketShow, packetConfig, pos, aliveTime, isFall, useCost, velocityX, velocityY, height);
		return towerDefenseInGamePacketShow;
	}

	private void InitializeSpawnedPacketMovement(TowerDefenseInGamePacketShow packet, bool isFall, double height, bool useRandf, Vector2? velocityOverride, out double velocityX, out double velocityY)
	{
		velocityX = 0.0;
		velocityY = -300.0;
		if (isFall)
		{
			packet.CreateTween().TweenProperty(packet, "global_position:y", height, (height - (double)base.GlobalPosition.Y) / 25.0);
			return;
		}
		packet.height = 1.0;
		packet.moveComponent.gravity = 980.0;
		if (velocityOverride.HasValue)
		{
			velocityX = velocityOverride.Value.X;
			velocityY = velocityOverride.Value.Y;
		}
		else if (useRandf)
		{
			if ((double)GD.Randf() > 0.5)
			{
				velocityX = GD.RandRange(-80.0, -50.0);
			}
			else
			{
				velocityX = GD.RandRange(30.0, 80.0);
			}
		}
		packet.moveComponent.velocity = new Vector2((float)velocityX, (float)velocityY);
	}

	private void BindSpawnedPacketPicker(TowerDefenseInGamePacketShow packet)
	{
		PacketPickControl packetPickControl = Instance.GetPacketPickControl();
		if (packetPickControl != null)
		{
			packet.OnPressed += packetPickControl.PickPacket;
		}
	}

	private void PublishSpawnedPacket(TowerDefenseInGamePacketShow packet, TowerDefensePacketConfig packetConfig, Vector2 pos, double aliveTime, bool isFall, bool useCost, double velocityX, double velocityY, double height)
	{
		if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			TowerDefenseControlNew towerDefenseControlNew = Instance.currentControl;
			if (GodotObject.IsInstanceValid(towerDefenseControlNew))
			{
				int nextPacketSyncId = towerDefenseControlNew.GetNextPacketSyncId();
				packet.SetMeta("packet_sync_id", nextPacketSyncId);
				towerDefenseControlNew.RegisterSyncPacket(nextPacketSyncId, packet);
				MultiPlayerManager.Instance.SendPacketSpawn(nextPacketSyncId, packetConfig.saveKey, pos.X, pos.Y, aliveTime, isFall, useCost, (float)velocityX, (float)velocityY, packet.ZIndex, (float)height, packetConfig, packet.SunAccountId);
			}
		}
	}

	public TowerDefenseBattleFeaturePortal GetPortalFeature()
	{
		if (GodotObject.IsInstanceValid(currentControl) && currentControl.GetFeature(FeatureName_Portal) is TowerDefenseBattleFeaturePortal result)
		{
			return result;
		}
		return null;
	}

	public void PortalCreate(string shape, Vector4I posRange, double changeTime = 0.0)
	{
		GetPortalFeature()?.PortalCreate(shape, posRange, changeTime);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void ProtalCreate(string shape, Vector4I posRange, double changeTime = 0.0)
	{
		PortalCreate(shape, posRange, changeTime);
	}

	public void PortalChangePos()
	{
		GetPortalFeature()?.PortalChangePos();
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void ProtalChangePos()
	{
		PortalChangePos();
	}

	public static TowerDefenseProjectileConfig GetProjectileConfig(string projectileName)
	{
		if (ResourceManager.Instance == null)
		{
			ILogger logger = _logger;
			bool enabled;
			ZLoggerErrorInterpolatedStringHandler message = new ZLoggerErrorInterpolatedStringHandler(53, 0, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("GetProjectileConfig: ResourceManager.Instance is null");
			}
			logger.ZLogError(ref message);
			return null;
		}
		if (string.IsNullOrEmpty(projectileName) || !ResourceManager.Instance.PROJECTILE_CONFIG.ContainsKey(projectileName))
		{
			return null;
		}
		Resource resource = ResourceManager.Instance.PROJECTILE_CONFIG[projectileName];
		if (resource == null || !GodotObject.IsInstanceValid(resource))
		{
			return null;
		}
		if (!_projectileConfigRefCache.ContainsKey(projectileName))
		{
			_projectileConfigRefCache[projectileName] = resource as TowerDefenseProjectileConfig;
		}
		return resource as TowerDefenseProjectileConfig;
	}

	public TowerDefenseSunBase QXSunCreate(Vector2 pos, long sunNum, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.LAND, double height = 0.0, Vector2 velocity = default(Vector2), double gravity = 980.0, double moveStopTime = -1.0)
	{
		return CreateSunDrop(ObjectManagerConfig.OBJECT.SUN_QX, EconomyAccountId.Local, SunDropOwnershipPolicy.LegacySharedReplica, pos, sunNum, movingMethod, height, velocity, gravity, moveStopTime);
	}

	public TowerDefenseSunBase QXSunCreate(EconomyAccountId accountId, Vector2 pos, long sunNum, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.LAND, double height = 0.0, Vector2 velocity = default(Vector2), double gravity = 980.0, double moveStopTime = -1.0)
	{
		if (!CanCreateAccountOwnedSunDrop(accountId))
		{
			return null;
		}
		return CreateSunDrop(ObjectManagerConfig.OBJECT.SUN_QX, accountId, SunDropOwnershipPolicy.AccountOwned, pos, sunNum, movingMethod, height, velocity, gravity, moveStopTime);
	}

	public Array<TowerDefenseCharacter> _GetCleanCharacters()
	{
		return characterRegistry.GetCleanCharacters();
	}

	public Godot.Collections.Array _GetNodesInGroupCached(string groupName)
	{
		long physicsFrames = (long)Engine.GetPhysicsFrames();
		if (physicsFrames != _groupCacheFrame)
		{
			_groupCacheFrame = physicsFrames;
			_groupCache.Clear();
		}
		if (!_groupCache.TryGetValue(groupName, out var value))
		{
			value = new Godot.Collections.Array();
			foreach (Node item in GetTree().GetNodesInGroup(groupName))
			{
				value.Add(item);
			}
			_groupCache[groupName] = value;
		}
		return value;
	}

	public TowerDefenseBattleFeatureSeedBank GetSeedBankFeature()
	{
		TowerDefenseControlNew towerDefenseControlNew = currentControl;
		if (towerDefenseControlNew != null)
		{
			return towerDefenseControlNew.GetFeature(FeatureName_SeedBank) as TowerDefenseBattleFeatureSeedBank;
		}
		return null;
	}

	public TowerDefenseEnum.LEVEL_SEEDBANK_METHOD GetCurrentPacketBankMethod()
	{
		TowerDefenseBattleFeatureSeedBank seedBankFeature = GetSeedBankFeature();
		if (GodotObject.IsInstanceValid(seedBankFeature?.config))
		{
			return seedBankFeature.config.method;
		}
		if (currentLevelConfig is TowerDefenseLevelConfig towerDefenseLevelConfig)
		{
			return towerDefenseLevelConfig.packetBankMethod;
		}
		return TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.NOONE;
	}

	public TowerDefenseInGameSeedBank GetSeedBank()
	{
		return GetSeedBankFeature()?.seedBank;
	}

	public static List<TowerDefensePacketChangeCost> GetChangeCostList()
	{
		if (Instance == null)
		{
			return new List<TowerDefensePacketChangeCost>();
		}
		TowerDefenseControlNew towerDefenseControlNew = Instance.currentControl;
		if (towerDefenseControlNew != null)
		{
			return towerDefenseControlNew.changeCostList;
		}
		return new List<TowerDefensePacketChangeCost>();
	}

	public bool ChangeCostAdd(TowerDefensePacketChangeCost changeCost)
	{
		return currentControl?.ChangeCostAdd(changeCost) ?? false;
	}

	public bool ChangeCostRemove(TowerDefensePacketChangeCost changeCost)
	{
		return currentControl?.ChangeCostRemove(changeCost) ?? false;
	}

	public int GetPacketSlotNum()
	{
		int num = 7;
		for (int i = 8; i < 17; i++)
		{
			if (GameSaveManager.Instance.GetFeatureValue($"PacketSlot{i}") <= 0)
			{
				break;
			}
			num++;
		}
		seedbankPacketMax = num;
		return num;
	}

	public void AddPacket(string packetName, TowerDefensePacketOverride override_ = null)
	{
		TowerDefenseInGameSeedBank seedBank = GetSeedBank();
		if (!GodotObject.IsInstanceValid(seedBank) || string.IsNullOrWhiteSpace(packetName))
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = GetPacketConfig(packetName);
		if (packetConfig != null)
		{
			if (GodotObject.IsInstanceValid(override_))
			{
				packetConfig._override = override_;
			}
			seedBank.AddPacket(packetConfig, isStart: true);
		}
	}

	public Array<TowerDefenseInGamePacketShow> GetSeedBankList()
	{
		TowerDefenseInGameSeedBank seedBank = GetSeedBank();
		if (GodotObject.IsInstanceValid(seedBank))
		{
			return seedBank.packetList;
		}
		return new Array<TowerDefenseInGamePacketShow>();
	}

	public static ShovelConfig GetShovel(string shovelName)
	{
		return (ShovelConfig)ResourceManager.Instance.SHOVELS[shovelName];
	}

	public static Godot.Collections.Array GetShovelList()
	{
		List<Variant> list = new List<Variant>();
		foreach (string key in ResourceManager.Instance.SHOVELS.Keys)
		{
			list.Add(key);
		}
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (Variant item in list)
		{
			array.Add(item);
		}
		return array;
	}

	public async void YBCreate(Vector2 pos, int num, double height = 0.0, Vector2 velocity = default(Vector2), double gravity = 0.0, bool _collect = false)
	{
		Node2D characterNode = GetCharacterNode();
		while (num >= 1000)
		{
			TowerDefenseGroundItemBase towerDefenseGroundItemBase = Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_YB2, pos, height, velocity, gravity);
			towerDefenseGroundItemBase.gridPos = new Vector2I(towerDefenseGroundItemBase.gridPos.X, 200);
			towerDefenseGroundItemBase.Reparent(characterNode, keepGlobalTransform: false);
			if (_collect)
			{
				towerDefenseGroundItemBase.Collection();
			}
			num -= 1000;
			await ToSignal(GetTree().CreateTimer(0.1, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		}
		while (num >= 50)
		{
			TowerDefenseGroundItemBase towerDefenseGroundItemBase2 = Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_YB1, pos, height, velocity, gravity);
			towerDefenseGroundItemBase2.gridPos = new Vector2I(towerDefenseGroundItemBase2.gridPos.X, 200);
			towerDefenseGroundItemBase2.Reparent(characterNode, keepGlobalTransform: false);
			if (_collect)
			{
				towerDefenseGroundItemBase2.Collection();
			}
			num -= 50;
			await ToSignal(GetTree().CreateTimer(0.1, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		}
		while (num >= 10)
		{
			TowerDefenseGroundItemBase towerDefenseGroundItemBase3 = Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_TQ, pos, height, velocity, gravity);
			towerDefenseGroundItemBase3.gridPos = new Vector2I(towerDefenseGroundItemBase3.gridPos.X, 200);
			towerDefenseGroundItemBase3.Reparent(characterNode, keepGlobalTransform: false);
			if (_collect)
			{
				towerDefenseGroundItemBase3.Collection();
			}
			num -= 10;
			await ToSignal(GetTree().CreateTimer(0.1, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		}
	}

	public static int PublishSpawnedCharacter(string packetName, TowerDefenseCharacter character, bool useCreate, double riseDuration = 0.0, bool walkAfterSpawn = false, string sizeVal = "", Dictionary spawnState = null)
	{
		if (!CanBeginSpawnedCharacterPublish(packetName, character, out var control))
		{
			return -1;
		}
		int nextSyncId = control.GetNextSyncId();
		PublishSpawnedCharacterWhenReady(packetName, character, control, nextSyncId, useCreate, riseDuration, walkAfterSpawn, sizeVal, spawnState, 3);
		return nextSyncId;
	}

	private static void BroadcastSpawnedCharacter(string packetName, TowerDefenseCharacter character, TowerDefenseControlNew control, int syncId, bool useCreate, double riseDuration, bool walkAfterSpawn, string sizeVal, Dictionary spawnState)
	{
		double hitpointScale = (GodotObject.IsInstanceValid(character.instance) ? character.instance.hitpointScale : 1.0);
		double scaleVal = (GodotObject.IsInstanceValid(character.transformPoint) ? ((double)character.transformPoint.Scale.X) : 1.0);
		bool hypnoses = (GodotObject.IsInstanceValid(character.instance) && character.instance.hypnoses) || (spawnState?.GetValueOrDefault("hypnoses", false).AsBool() ?? false);
		string economyOwner = character.EconomyOwnerAccountId.ToString();
		Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
		control.RegisterSyncCharacter(syncId, character);
		MultiPlayerManager.Instance.SendSpawnCharacterAt(packetName, character.gridPos.X, character.gridPos.Y, syncId, hitpointScale, scaleVal, hypnoses, riseDuration, useCreate, logicalGlobalPosition.X, logicalGlobalPosition.Y, walkAfterSpawn, character.groundHeight, sizeVal, spawnState, economyOwner);
	}

	private static bool CanBeginSpawnedCharacterPublish(string packetName, TowerDefenseCharacter character, out TowerDefenseControlNew control)
	{
		control = CurrentControl;
		if (!Global.IsMultiplayerMode || !MultiPlayerManager.IsHost)
		{
			return false;
		}
		if (string.IsNullOrEmpty(packetName) || !GodotObject.IsInstanceValid(character) || !GodotObject.IsInstanceValid(control) || !GodotObject.IsInstanceValid(MultiPlayerManager.Instance))
		{
			return false;
		}
		return true;
	}

	private static bool CanContinueSpawnedCharacterPublish(TowerDefenseCharacter character, TowerDefenseControlNew control)
	{
		if (!Global.IsMultiplayerMode || !MultiPlayerManager.IsHost)
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(control) || control != CurrentControl)
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(character) || character.IsQueuedForDeletion())
		{
			return false;
		}
		return GodotObject.IsInstanceValid(MultiPlayerManager.Instance);
	}

	private static void PublishSpawnedCharacterWhenReady(string packetName, TowerDefenseCharacter character, TowerDefenseControlNew control, int syncId, bool useCreate, double riseDuration, bool walkAfterSpawn, string sizeVal, Dictionary spawnState, int readyRetries)
	{
		if (CanContinueSpawnedCharacterPublish(character, control))
		{
			if (!character.IsNodeReady())
			{
				DeferSpawnedCharacterPublish(packetName, character, control, syncId, useCreate, riseDuration, walkAfterSpawn, sizeVal, spawnState, readyRetries);
			}
			else
			{
				BroadcastSpawnedCharacter(packetName, character, control, syncId, useCreate, riseDuration, walkAfterSpawn, sizeVal, spawnState);
			}
		}
	}

	private static void DeferSpawnedCharacterPublish(string packetName, TowerDefenseCharacter character, TowerDefenseControlNew control, int syncId, bool useCreate, double riseDuration, bool walkAfterSpawn, string sizeVal, Dictionary spawnState, int readyRetries)
	{
		if (readyRetries > 0)
		{
			Callable.From(delegate
			{
				PublishSpawnedCharacterWhenReady(packetName, character, control, syncId, useCreate, riseDuration, walkAfterSpawn, sizeVal, spawnState, readyRetries - 1);
			}).CallDeferred();
		}
	}

	public void LuckyBagCreate(Vector2 pos, double height = 0.0, Vector2 velocity = default(Vector2), double gravity = 0.0)
	{
		Node2D characterNode = GetCharacterNode();
		TowerDefenseGroundItemBase towerDefenseGroundItemBase = Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_LUCKY_BAG, pos, height, velocity, gravity);
		towerDefenseGroundItemBase.gridPos = new Vector2I(towerDefenseGroundItemBase.gridPos.X, 200);
		towerDefenseGroundItemBase.Reparent(characterNode, keepGlobalTransform: false);
	}

	public void GoldShardCreate(Vector2 pos, double height = 0.0, Vector2 velocity = default(Vector2), double gravity = 0.0)
	{
		Node2D characterNode = GetCharacterNode();
		TowerDefenseGroundItemBase towerDefenseGroundItemBase = Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_GOLD_SHARD, pos, height, velocity, gravity);
		towerDefenseGroundItemBase.gridPos = new Vector2I(towerDefenseGroundItemBase.gridPos.X, 200);
		towerDefenseGroundItemBase.Reparent(characterNode, keepGlobalTransform: false);
	}

	public TowerDefenseSunBase JalapenoSunCreate(Vector2 pos, long sunNum, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.LAND, double height = 0.0, Vector2 velocity = default(Vector2), double gravity = 980.0, double moveStopTime = -1.0)
	{
		return CreateSunDrop(ObjectManagerConfig.OBJECT.SUN_JALAPENO, EconomyAccountId.Local, SunDropOwnershipPolicy.LegacySharedReplica, pos, sunNum, movingMethod, height, velocity, gravity, moveStopTime);
	}

	public TowerDefenseSunBase JalapenoSunCreate(EconomyAccountId accountId, Vector2 pos, long sunNum, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod = TowerDefenseEnum.SUN_MOVING_METHOD.LAND, double height = 0.0, Vector2 velocity = default(Vector2), double gravity = 980.0, double moveStopTime = -1.0)
	{
		if (!CanCreateAccountOwnedSunDrop(accountId))
		{
			return null;
		}
		return CreateSunDrop(ObjectManagerConfig.OBJECT.SUN_JALAPENO, accountId, SunDropOwnershipPolicy.AccountOwned, pos, sunNum, movingMethod, height, velocity, gravity, moveStopTime);
	}

	public static Node2D _GetCharacterNode()
	{
		return GetCharacterNode();
	}

	public static bool _IsGameRunning()
	{
		if (Instance == null)
		{
			return false;
		}
		return Instance.IsGameRunning();
	}

	public EconomyAccountId GetLocalSunAccountId()
	{
		return GetSunFeature()?.LocalAccountId ?? EconomyAccountId.Local;
	}

	public bool TryResolveTrustedPeerSunAccount(string trustedSenderPeerId, out EconomyAccountId accountId)
	{
		accountId = default(EconomyAccountId);
		if (string.IsNullOrWhiteSpace(trustedSenderPeerId))
		{
			return false;
		}
		MultiPlayerManager instance = MultiPlayerManager.Instance;
		if (instance != null && string.Equals(trustedSenderPeerId, instance.peerId, StringComparison.Ordinal))
		{
			accountId = EconomyAccountId.Local;
			return true;
		}
		if (!EconomyAccountId.TryFromPeerId(trustedSenderPeerId, out accountId))
		{
			return false;
		}
		return RegisterSunAccount(accountId);
	}

	public bool RegisterSunAccount(EconomyAccountId accountId, long? initialSun = null)
	{
		TowerDefenseBattleFeatureSun sunFeature = GetSunFeature();
		if (sunFeature == null)
		{
			return false;
		}
		long initialSun2 = initialSun ?? sunFeature.config?.begin ?? 0;
		return sunFeature.RegisterAccount(accountId, initialSun2);
	}

	private bool CanCreateAccountOwnedSunDrop(EconomyAccountId accountId)
	{
		if (accountId.IsValid && TryGetSun(accountId, out var _))
		{
			return true;
		}
		GD.PushWarning($"Rejected Sun drop for invalid or unregistered account {accountId}.");
		return false;
	}

	private TowerDefenseSunBase CreateSunDrop(ObjectManagerConfig.OBJECT poolKey, EconomyAccountId accountId, SunDropOwnershipPolicy ownershipPolicy, Vector2 pos, long sunNum, TowerDefenseEnum.SUN_MOVING_METHOD movingMethod, double height, Vector2 velocity, double gravity, double moveStopTime)
	{
		if (velocity == default(Vector2))
		{
			velocity = new Vector2((float)GD.RandRange(-50.0, 50.0), -400f);
		}
		Node node = ObjectManager.PoolPop(poolKey, GetCharacterNode());
		if (!(node is TowerDefenseSunBase towerDefenseSunBase))
		{
			if (GodotObject.IsInstanceValid(node))
			{
				GD.PushWarning($"Pool entry {poolKey} is not a TowerDefenseSunBase; returning it to the pool.");
				ObjectManager.PoolPush(poolKey, node);
			}
			return null;
		}
		towerDefenseSunBase.GlobalPosition = pos;
		if (ownershipPolicy == SunDropOwnershipPolicy.AccountOwned)
		{
			towerDefenseSunBase.Init(accountId, sunNum, movingMethod, height, velocity, gravity, moveStopTime);
		}
		else
		{
			towerDefenseSunBase.Init(sunNum, movingMethod, height, velocity, gravity, moveStopTime);
		}
		return towerDefenseSunBase;
	}

	public TowerDefenseBattleFeatureSun GetSunFeature()
	{
		TowerDefenseControlNew towerDefenseControlNew = currentControl;
		if (towerDefenseControlNew != null)
		{
			return towerDefenseControlNew.GetFeature(FeatureName_Sun) as TowerDefenseBattleFeatureSun;
		}
		return null;
	}

	public bool TryGetSun(EconomyAccountId accountId, out long balance)
	{
		balance = 0L;
		return GetSunFeature()?.TryGetSun(accountId, out balance) ?? false;
	}

	public long GetSun(EconomyAccountId accountId)
	{
		if (!TryGetSun(accountId, out var balance))
		{
			return -1L;
		}
		return balance;
	}

	public bool CanAffordSun(EconomyAccountId accountId, long amount)
	{
		if (TryGetSun(accountId, out var balance))
		{
			if (amount >= 0)
			{
				return balance >= amount;
			}
			return true;
		}
		return false;
	}

	public bool CreditSun(EconomyAccountId accountId, long amount, SunTransactionReason reason = SunTransactionReason.Reward)
	{
		return GetSunFeature()?.CreditSun(accountId, amount, reason) ?? false;
	}

	internal bool ApplyCollectedSunValue(EconomyAccountId accountId, long value)
	{
		return GetSunFeature()?.ApplyCollectedSunValue(accountId, value) ?? false;
	}

	public bool TrySpendSun(EconomyAccountId accountId, long amount, SunTransactionReason reason = SunTransactionReason.Cost)
	{
		return GetSunFeature()?.TrySpendSun(accountId, amount, reason) ?? false;
	}

	public bool TryBeginSunSpend(EconomyAccountId accountId, long amount, out SunSpendReceipt receipt, SunTransactionReason reason = SunTransactionReason.Cost)
	{
		receipt = null;
		return GetSunFeature()?.TryBeginSunSpend(accountId, amount, out receipt, reason) ?? false;
	}

	public bool SetSun(EconomyAccountId accountId, long value, SunTransactionReason reason = SunTransactionReason.Debug)
	{
		return GetSunFeature()?.SetSun(accountId, value, reason) ?? false;
	}

	public static TutorialConfig GetTutorial(string tutorialName)
	{
		if (string.IsNullOrEmpty(tutorialName))
		{
			return null;
		}
		if (ResourceManager.Instance.TUTORIALS.TryGetValue(tutorialName, out var value) && value is TutorialConfig result)
		{
			return result;
		}
		GD.PushWarning("[Tutorial] Tutorial '" + tutorialName + "' is unavailable.");
		return null;
	}

	public string PickRandomZomie(Godot.Collections.Array zombiePool)
	{
		Array<WeightPickItemBase> array = new Array<WeightPickItemBase>();
		foreach (Variant item2 in zombiePool)
		{
			string text = (string)item2;
			TowerDefensePacketConfig packetConfig = GetPacketConfig(text);
			TowerDefenseCharacterConfig characterConfig = packetConfig.characterConfig;
			TowerDefenseLevelSpawnConfig towerDefenseLevelSpawnConfig = new TowerDefenseLevelSpawnConfig
			{
				zombie = text
			};
			if (characterConfig is TowerDefenseZombieConfig)
			{
				int weight = packetConfig.GetWeight();
				WeightPickItemBase item = new WeightPickItemBase(towerDefenseLevelSpawnConfig, weight);
				array.Add(item);
			}
		}
		if (array.Count > 0)
		{
			return ((TowerDefenseLevelSpawnConfig)WeightPickMathine.Pick(array).item.AsGodotObject()).zombie;
		}
		return "";
	}

	public TowerDefenseManager()
	{
		StartupLoadDiagnostics.Mark("autoload.constructed/TowerDefenseManager");
	}
}
