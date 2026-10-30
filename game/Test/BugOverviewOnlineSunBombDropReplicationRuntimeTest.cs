using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewOnlineSunBombDropReplicationRuntimeTest : Node
{
	private sealed class DropSnapshot
	{
		public string Kind = "";

		public long Amount;

		public Vector2 Position;

		public int MovingMethod;

		public double Height;

		public Vector2 Velocity;

		public double Gravity;

		public string EconomyOwner = "";

		public int OwnershipPolicy;
	}

	private const string SunBombScenePath = "res://Asset/Anime/Character/Plant/Chapter1/SunBomb/Scene/TowerDefensePlantSunBomb.tscn";

	private const string SunBombPacketPath = "res://Asset/Anime/Character/Plant/Chapter1/SunBomb/Packet/PlantSunBomb.tres";

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string ZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const int ZombieSyncId = 7101;

	private const int SunBombSyncId = 7102;

	private const string SunCreateOperationName = "sun_create";

	private const string ResourceSpawnInstanceId = "character.resource_spawn";

	private const string ResourceSpawnTypeId = "ResourceSpawnComponent";

	private static readonly Vector2I TestGrid = new Vector2I(5, 3);

	private const string TestOwnerPeerId = "sun-bomb-owner";

	private int _checks;

	private int _failures;

	private string _role = "";

	private int _port;

	private int _messageOrder;

	private int _firstOperationOrder = -1;

	private int _firstDestroyOrder = -1;

	private int _componentOperationMessages;

	private int _destroyMessages;

	private int _clientReadyMessages;

	private bool _destroyExplodeFlag;

	private Dictionary _firstEnvelope;

	private Dictionary _firstOperation;

	private DropSnapshot _dropAtFirstOperation;

	private int _verifiedDropCount;

	private bool _exactDropMatches;

	private TowerDefenseBattleNetworkHost _networkHost;

	private TowerDefenseZombie _zombie;

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		MultiPlayerManager multiplayer = MultiPlayerManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		OnlineSunBombRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseBattleFeatureSun sunFeature = null;
		bool subscribed = false;
		bool hostDestroySubscribed = false;
		try
		{
			_ = 8;
			try
			{
				ParseArguments();
				Check((_role == "host" || _role == "client") && _port > 0, "The fixture requires --role=host/client and a positive --port.");
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(multiplayer) && GodotObject.IsInstanceValid(ResourceManager.Instance) && GodotObject.IsInstanceValid(ObjectManager.Instance), "Required production autoloads must be available in both processes.");
				if (_failures > 0 || !RegisterRealFixtures())
				{
					return;
				}
				bool flag = StartTransport(multiplayer);
				Check(flag, (_role == "host") ? "CreateMatch() must bind the requested real ENet port." : "JoinMatch() must start a real ENet client.");
				if (!flag)
				{
					return;
				}
				if (_role == "host")
				{
					GD.Print($"ONLINE_SUN_BOMB_HOST_LISTENING port={_port}");
				}
				control = new OnlineSunBombRuntimeControlStub
				{
					Name = "OnlineSunBombRuntimeControl",
					isGameRunning = true,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig()
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				manager.gridBeginPos = Vector2.Zero;
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				sunFeature = new TowerDefenseBattleFeatureSun
				{
					control = control
				};
				sunFeature.Init(new Dictionary { ["Begin"] = 0L });
				control.featureDictionary[new StringName("Sun")] = sunFeature;
				_networkHost = TowerDefenseBattleNetworkHost.TryCreate(new TowerDefenseBattleNetworkContext(control));
				Check(_networkHost != null && BindNetworkHost(control, _networkHost), "The fixture must bind the production TowerDefenseBattleNetworkHost to the control.");
				if (_networkHost == null)
				{
					return;
				}
				multiplayer.OnMatchStateReceived += _networkHost.HandleLegacyMessage;
				multiplayer.OnMatchStateReceived += OnMatchStateReceived;
				subscribed = true;
				bool flag2 = await WaitUntil(() => multiplayer.IsConnect() && (_role == "client" || multiplayer.matchMembers.Count >= 2), 600);
				Check(flag2, "The two isolated processes must establish a real ENet connection.");
				if (!flag2)
				{
					return;
				}
				Check(manager.TryResolveTrustedPeerSunAccount("sun-bomb-owner", out var accountId) && manager.TryGetSun(accountId, out var _), "Both peers must register the shared ledger through the production authenticated-peer resolver.");
				TowerDefensePlant sunBomb = Instantiate<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter1/SunBomb/Scene/TowerDefensePlantSunBomb.tscn");
				_zombie = Instantiate<TowerDefenseZombie>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
				Check(GodotObject.IsInstanceValid(sunBomb) && sunBomb.config?.name == "PlantSunBomb", "The fixture must instantiate the reported real Sun Bomb scene.");
				Check(GodotObject.IsInstanceValid(_zombie) && _zombie.config?.name == "ZombieNormal", "The fixture must instantiate the real normal zombie scene.");
				if (!GodotObject.IsInstanceValid(sunBomb) || !GodotObject.IsInstanceValid(_zombie))
				{
					return;
				}
				sunBomb.inGame = true;
				sunBomb.editorPreviewMode = false;
				sunBomb.gridPos = TestGrid;
				sunBomb.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(TestGrid);
				sunBomb.syncId = 7102;
				_zombie.inGame = true;
				_zombie.editorPreviewMode = false;
				_zombie.gridPos = TestGrid;
				_zombie.GlobalPosition = sunBomb.GlobalPosition;
				_zombie.syncId = 7101;
				Check(sunBomb.TryAssignEconomyOwner(accountId) && _zombie.TryAssignEconomyOwner(accountId), "Both real characters must retain the same account identity on both peers.");
				control.characterNode.AddChild(sunBomb, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(_zombie, forceReadableName: false, InternalMode.Disabled);
				control.RegisterSyncCharacter(7100, _zombie);
				control.RegisterSyncCharacter(7101, _zombie);
				control.RegisterSyncCharacter(7102, sunBomb);
				await WaitFrames(8);
				ExplodeComponent explode = sunBomb.componentManager?.GetRuntime<ExplodeComponent>("character.explode");
				ResourceSpawnComponent resourceSpawnComponent = _zombie.componentManager?.GetRuntime<ResourceSpawnComponent>("character.resource_spawn");
				Check(explode != null && !explode.IsReleased && explode.explodeEvent.Count == 2 && explode.explodeEvent[0] is TowerDefenseCharacterEventExplodeHurt && explode.explodeEvent[1] is TowerDefenseCharacterEventSunCreate, "The real Sun Bomb must execute explosion damage before its death-sun event.");
				Check(resourceSpawnComponent != null && !resourceSpawnComponent.IsReleased && resourceSpawnComponent.ComponentDefinition?.InstanceId == "character.resource_spawn" && resourceSpawnComponent.ComponentDefinition?.ComponentTypeId == "ResourceSpawnComponent", "The real zombie must expose the stable production resource-spawn identity.");
				string resourceSpawnInstanceId;
				string resourceSpawnTypeId;
				if (explode != null && !explode.IsReleased && resourceSpawnComponent != null && !resourceSpawnComponent.IsReleased)
				{
					resourceSpawnInstanceId = resourceSpawnComponent.ComponentDefinition.InstanceId;
					resourceSpawnTypeId = resourceSpawnComponent.ComponentDefinition.ComponentTypeId;
					if (_role == "host")
					{
						_zombie.OnDestroy += OnHostZombieDestroyed;
						hostDestroySubscribed = true;
					}
					Check(_zombie.syncId == 7101 && sunBomb.syncId == 7102 && !control._syncCharacters.ContainsKey(7100) && control._syncCharacters.TryGetValue(7101, out var value) && value == _zombie, "Both peers must replace a stale identity and retain one identical real character sync ID.");
					if (_role == "client")
					{
						multiplayer.SendClientReady();
						Check(condition: true, "The client must send the production CLIENT_READY handshake.");
						goto IL_0b94;
					}
					bool flag3 = await WaitUntil(() => _clientReadyMessages >= 1, 600);
					Check(flag3, "The host must receive the client's production CLIENT_READY handshake.");
					if (flag3)
					{
						await WaitFrames(4);
						explode.Explode();
						Check(condition: true, "The host must invoke the real Sun Bomb ExplodeComponent.");
						await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
						Check(CaptureOnlySunDrop() != null, "The host explosion event pass must create one real authoritative Sun drop.");
						goto IL_0b94;
					}
				}
				goto end_IL_012f;
				IL_0b94:
				bool flag4 = await WaitUntil(delegate
				{
					if (!(_role == "client"))
					{
						if (_destroyMessages >= 1)
						{
							return CountSunDrops() == 1;
						}
						return false;
					}
					return _componentOperationMessages >= 1 && _destroyMessages >= 1;
				}, 600);
				Check(flag4, "The client must receive one real sun_create and its target destroy; the host must retain one local drop and target destroy.");
				if (!flag4)
				{
					goto end_IL_012f;
				}
				await WaitFrames(8);
				ValidateTransportAndDrop(resourceSpawnInstanceId, resourceSpawnTypeId, accountId);
				if (_role == "client")
				{
					multiplayer.SendClientReady();
					await WaitFrames(12);
				}
				else
				{
					Check(await WaitUntil(() => _clientReadyMessages >= 2, 600), "The host must keep the match alive until the client finishes validating its drop.");
				}
				goto end_IL_0100;
				end_IL_012f:;
			}
			catch (Exception value2)
			{
				_failures++;
				GD.PushError($"[OnlineSunBomb:{_role}] Unexpected exception: {value2}");
				goto end_IL_0100;
			}
			return;
			end_IL_0100:;
		}
		finally
		{
			if (hostDestroySubscribed && GodotObject.IsInstanceValid(_zombie))
			{
				_zombie.OnDestroy -= OnHostZombieDestroyed;
			}
			if (subscribed && GodotObject.IsInstanceValid(multiplayer))
			{
				multiplayer.OnMatchStateReceived -= OnMatchStateReceived;
				if (_networkHost != null)
				{
					multiplayer.OnMatchStateReceived -= _networkHost.HandleLegacyMessage;
				}
			}
			_networkHost?.Dispose();
			_networkHost = null;
			if (GodotObject.IsInstanceValid(multiplayer))
			{
				multiplayer.LeaveMatch();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
				manager.characterRegistry?.Clear();
			}
			sunFeature?.Destroy();
			mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			ObjectManager.Instance?.Clear();
			RestoreRealFixtures();
			ResourceManager.Instance?.ReleaseTransientResources();
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			await WaitFramesWithoutNetwork(16);
		}
		bool flag5 = _failures == 0;
		string value3 = ((_role == "host") ? "ONLINE_SUN_BOMB_HOST_RESULT" : "ONLINE_SUN_BOMB_CLIENT_RESULT");
		string value4 = ((_role == "client") ? (_firstOperationOrder >= 0 && _firstOperationOrder < _firstDestroyOrder).ToString() : "ClientOnly");
		string value5 = ((_role == "client") ? _componentOperationMessages.ToString() : "ClientOnly");
		string value6 = ((_role == "client") ? _exactDropMatches.ToString() : "ClientOnly");
		GD.Print($"{value3} passed={flag5} checks={_checks} failures={_failures} op_messages={value5} destroy_messages={_destroyMessages} drop_count={_verifiedDropCount} op_before_destroy={value4} exact_drop={value6}");
		GetTree().Quit((!flag5) ? 2 : 0);
	}

	private void ValidateTransportAndDrop(string resourceSpawnInstanceId, string resourceSpawnTypeId, EconomyAccountId accountId)
	{
		Check(_destroyMessages == 1, "Exactly one production CHARACTER_DESTROY must follow the Sun operation.");
		if (_role == "client")
		{
			Check(_componentOperationMessages == 1, "The client must receive exactly one authoritative sun_create operation.");
			Check(_firstOperationOrder >= 0 && _firstDestroyOrder > _firstOperationOrder, "The client must observe the original reliable sun_create operation before CHARACTER_DESTROY.");
			Check(_firstEnvelope != null && _firstEnvelope.GetValueOrDefault("sync_id", -1).AsInt32() == 7101 && _firstEnvelope.GetValueOrDefault("component_instance_id", "").AsString() == resourceSpawnInstanceId && _firstEnvelope.GetValueOrDefault("component_type_id", "").AsString() == resourceSpawnTypeId && _firstEnvelope.GetValueOrDefault("operation_name", "").AsString() == "sun_create", "The envelope must address the real zombie's stable ResourceSpawn runtime.");
		}
		else
		{
			Check(_firstDestroyOrder >= 0, "The host must observe the real local zombie destruction; operation ordering is client-only evidence.");
		}
		_verifiedDropCount = CountSunDrops();
		Check(_verifiedDropCount == 1, "Each peer must retain exactly one real Sun drop after the explosion.");
		if (_role == "client")
		{
			ValidateClientDrop(accountId);
		}
		Check(_destroyExplodeFlag, "Deferred CHARACTER_DESTROY must retain the original explosion flag.");
		int condition;
		if (!GodotObject.IsInstanceValid(_zombie) || _zombie.isDestroy)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (currentControl != null)
			{
				condition = ((!currentControl._syncCharacters.ContainsKey(7101)) ? 1 : 0);
				goto IL_01d4;
			}
		}
		condition = 0;
		goto IL_01d4;
		IL_01d4:
		Check((byte)condition != 0, "The zombie must be destroyed only after its Sun operation and removed from sync state.");
	}

	private void OnMatchStateReceived(string opCode, string data, string senderId)
	{
		if (opCode == "client_ready" && _role == "host")
		{
			_clientReadyMessages++;
		}
		else if (opCode == "character_component_operation")
		{
			if (_role != "client")
			{
				return;
			}
			Variant variant = Json.ParseString(data);
			if (variant.VariantType == Variant.Type.Dictionary)
			{
				Dictionary dictionary = variant.AsGodotDictionary();
				if (dictionary.GetValueOrDefault("sync_id", -1).AsInt32() == 7101 && !(dictionary.GetValueOrDefault("component_instance_id", "").AsString() != "character.resource_spawn") && !(dictionary.GetValueOrDefault("component_type_id", "").AsString() != "ResourceSpawnComponent") && !(dictionary.GetValueOrDefault("operation_name", "").AsString() != "sun_create"))
				{
					Dictionary operation = (dictionary.ContainsKey("data") ? dictionary["data"].AsGodotDictionary() : new Dictionary());
					RecordComponentOperation(dictionary, operation, CaptureOnlySunDrop());
				}
			}
		}
		else
		{
			if (!(opCode == "character_destroy") || _role != "client")
			{
				return;
			}
			CharacterDestroyDto characterDestroyDto = MatchStateSerializer.Deserialize<CharacterDestroyDto>(data);
			if (characterDestroyDto != null && characterDestroyDto.sync_id == 7101)
			{
				_messageOrder++;
				_destroyMessages++;
				if (_firstDestroyOrder < 0)
				{
					_firstDestroyOrder = _messageOrder;
				}
				_destroyExplodeFlag = characterDestroyDto.is_explode;
			}
		}
	}

	private void OnHostZombieDestroyed(TowerDefenseCharacter character)
	{
		if (!(_role != "host") && character == _zombie)
		{
			_messageOrder++;
			_destroyMessages++;
			if (_firstDestroyOrder < 0)
			{
				_firstDestroyOrder = _messageOrder;
			}
			_destroyExplodeFlag = character.isExplode;
		}
	}

	private void RecordComponentOperation(Dictionary envelope, Dictionary operation, DropSnapshot drop)
	{
		_messageOrder++;
		_componentOperationMessages++;
		if (_firstOperationOrder < 0)
		{
			_firstOperationOrder = _messageOrder;
		}
		if (_firstEnvelope == null)
		{
			_firstEnvelope = envelope.Duplicate(deep: true);
			_firstOperation = operation.Duplicate(deep: true);
			_dropAtFirstOperation = drop;
		}
	}

	private void ValidateClientDrop(EconomyAccountId accountId)
	{
		DropSnapshot dropAtFirstOperation = _dropAtFirstOperation;
		Dictionary firstOperation = _firstOperation;
		Check(dropAtFirstOperation != null && firstOperation != null, "The production network handler must create the real drop before the observer runs.");
		if (dropAtFirstOperation != null && firstOperation != null)
		{
			bool flag = dropAtFirstOperation.Kind == firstOperation.GetValueOrDefault("kind", "").AsString() && dropAtFirstOperation.Amount == firstOperation.GetValueOrDefault("amount", 0L).AsInt64();
			bool flag2 = dropAtFirstOperation.Position.IsEqualApprox(new Vector2(firstOperation.GetValueOrDefault("position_x", 0f).AsSingle(), firstOperation.GetValueOrDefault("position_y", 0f).AsSingle()));
			bool flag3 = dropAtFirstOperation.MovingMethod == firstOperation.GetValueOrDefault("moving_method", -1).AsInt32() && Mathf.IsEqualApprox((float)dropAtFirstOperation.Height, firstOperation.GetValueOrDefault("height", 0f).AsSingle()) && dropAtFirstOperation.Velocity.IsEqualApprox(new Vector2(firstOperation.GetValueOrDefault("velocity_x", 0f).AsSingle(), firstOperation.GetValueOrDefault("velocity_y", 0f).AsSingle())) && Mathf.IsEqualApprox((float)dropAtFirstOperation.Gravity, firstOperation.GetValueOrDefault("gravity", 0f).AsSingle());
			bool flag4 = dropAtFirstOperation.EconomyOwner == accountId.ToString() && dropAtFirstOperation.EconomyOwner == firstOperation.GetValueOrDefault("economy_owner", "").AsString() && dropAtFirstOperation.OwnershipPolicy == 1 && dropAtFirstOperation.OwnershipPolicy == firstOperation.GetValueOrDefault("ownership_policy", -1).AsInt32();
			Check(flag, "The real drop kind and amount must match the host operation.");
			Check(flag2, "The real drop position must match the host operation.");
			Check(flag3, "The real drop movement, host-randomized velocity, and gravity must match without a client roll.");
			Check(flag4, "The exact account-owned drop ownership must survive replication.");
			_exactDropMatches = flag && flag2 && flag3 && flag4;
		}
	}

	private bool RegisterRealFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		RegisterPacket(instance, "PlantSunBomb", "res://Asset/Anime/Character/Plant/Chapter1/SunBomb/Packet/PlantSunBomb.tres");
		RegisterCharacter(instance, "PlantSunBomb", "res://Asset/Anime/Character/Plant/Chapter1/SunBomb/Scene/TowerDefensePlantSunBomb.tscn");
		RegisterPacket(instance, "ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
		RegisterCharacter(instance, "ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
		if (instance.TOWERDEFENSE_PACKETS["PlantSunBomb"] is TowerDefensePacketConfig && instance.TOWERDEFENSE_CHARCATERS["PlantSunBomb"] is PackedScene && instance.TOWERDEFENSE_PACKETS["ZombieNormal"] is TowerDefensePacketConfig)
		{
			return instance.TOWERDEFENSE_CHARCATERS["ZombieNormal"] is PackedScene;
		}
		return false;
	}

	private void RegisterPacket(ResourceManager resources, string key, string path)
	{
		if (resources.TOWERDEFENSE_PACKETS.TryGetValue(key, out var value))
		{
			_previousPackets[key] = value;
		}
		else
		{
			_missingPackets.Add(key);
		}
		resources.TOWERDEFENSE_PACKETS[key] = ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore);
	}

	private void RegisterCharacter(ResourceManager resources, string key, string path)
	{
		if (resources.TOWERDEFENSE_CHARCATERS.TryGetValue(key, out var value))
		{
			_previousCharacters[key] = value;
		}
		else
		{
			_missingCharacters.Add(key);
		}
		resources.TOWERDEFENSE_CHARCATERS[key] = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
	}

	private void RestoreRealFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return;
		}
		foreach (string missingPacket in _missingPackets)
		{
			instance.TOWERDEFENSE_PACKETS.Remove(missingPacket);
		}
		foreach (KeyValuePair<string, Resource> previousPacket in _previousPackets)
		{
			instance.TOWERDEFENSE_PACKETS[previousPacket.Key] = previousPacket.Value;
		}
		foreach (string missingCharacter in _missingCharacters)
		{
			instance.TOWERDEFENSE_CHARCATERS.Remove(missingCharacter);
		}
		foreach (KeyValuePair<string, Resource> previousCharacter in _previousCharacters)
		{
			instance.TOWERDEFENSE_CHARCATERS[previousCharacter.Key] = previousCharacter.Value;
		}
	}

	private bool StartTransport(MultiPlayerManager multiplayer)
	{
		if (_role == "host")
		{
			FieldInfo field = typeof(MultiPlayerManager).GetField("_currentPort", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field == null)
			{
				return false;
			}
			field.SetValue(multiplayer, _port);
			if (multiplayer.CreateMatch())
			{
				return multiplayer.CurrentPort == _port;
			}
			return false;
		}
		return multiplayer.JoinMatch($"127.0.0.1:{_port}");
	}

	private static bool BindNetworkHost(TowerDefenseControlNew control, TowerDefenseBattleNetworkHost networkHost)
	{
		FieldInfo field = typeof(TowerDefenseControlNew).GetField("_battleNetworkHost", BindingFlags.Instance | BindingFlags.NonPublic);
		if (field == null)
		{
			return false;
		}
		field.SetValue(control, networkHost);
		return true;
	}

	private void ParseArguments()
	{
		string[] cmdlineUserArgs = OS.GetCmdlineUserArgs();
		foreach (string text in cmdlineUserArgs)
		{
			if (text.StartsWith("--role=", StringComparison.Ordinal))
			{
				string text2 = text;
				int length = "--role=".Length;
				_role = text2.Substring(length, text2.Length - length).Trim().ToLowerInvariant();
			}
			else if (text.StartsWith("--port=", StringComparison.Ordinal))
			{
				string text2 = text;
				int length = "--port=".Length;
				int.TryParse(text2.Substring(length, text2.Length - length), out _port);
			}
		}
	}

	private async Task<bool> WaitUntil(Func<bool> condition, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (condition())
			{
				return true;
			}
			await WaitFrames(1);
		}
		return condition();
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			_networkHost?.Process(1.0 / 60.0);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task WaitFramesWithoutNetwork(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig
			{
				gridNum = gridNum,
				gridBeginPos = Vector2.Zero,
				gridSize = new Vector2(100f, 76f),
				plantOffset = 50.0
			}
		};
		towerDefenseBattleFeatureMap.mapConfig = towerDefenseBattleFeatureMap.config;
		mapControl.mapFeature = towerDefenseBattleFeatureMap;
		towerDefenseBattleFeatureMap.plantGrid.Resize(gridNum.X + 1);
		for (int i = 0; i <= gridNum.X; i++)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			array.Resize(gridNum.Y + 1);
			for (int j = 1; j <= gridNum.Y; j++)
			{
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(i, j)
				};
				towerDefenseCellInstance.Init(new TowerDefenseCellConfig());
				array[j] = towerDefenseCellInstance;
			}
			towerDefenseBattleFeatureMap.plantGrid[i] = array;
		}
		towerDefenseBattleFeatureMap.iceCapList.Resize(gridNum.Y + 1);
		towerDefenseBattleFeatureMap.lineUse.Resize(gridNum.Y + 1);
		for (int k = 1; k <= gridNum.Y; k++)
		{
			towerDefenseBattleFeatureMap.lineUse[k] = true;
		}
		return towerDefenseBattleFeatureMap;
	}

	private static T Instantiate<T>(string path) where T : Node
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			return null;
		}
		return packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
	}

	private DropSnapshot CaptureOnlySunDrop()
	{
		TowerDefenseSunBase towerDefenseSunBase = null;
		int num = 0;
		foreach (Node item in GetTree().GetNodesInGroup("SunDropItem"))
		{
			if (item is TowerDefenseSunBase towerDefenseSunBase2 && GodotObject.IsInstanceValid(towerDefenseSunBase2) && !towerDefenseSunBase2.die && !towerDefenseSunBase2.isCollect)
			{
				towerDefenseSunBase = towerDefenseSunBase2;
				num++;
			}
		}
		if (num != 1 || !GodotObject.IsInstanceValid(towerDefenseSunBase))
		{
			return null;
		}
		return new DropSnapshot
		{
			Kind = ((towerDefenseSunBase.GetPoolKey() == ObjectManagerConfig.OBJECT.SUN_BRAIN) ? "brain_sun" : "sun"),
			Amount = towerDefenseSunBase.sunNum,
			Position = towerDefenseSunBase.GlobalPosition,
			MovingMethod = (int)towerDefenseSunBase.movingMethod,
			Height = towerDefenseSunBase.height,
			Velocity = (towerDefenseSunBase.moveComponent?.velocity ?? Vector2.Zero),
			Gravity = (towerDefenseSunBase.moveComponent?.gravity ?? 0.0),
			EconomyOwner = towerDefenseSunBase.AccountId.ToString(),
			OwnershipPolicy = (int)towerDefenseSunBase.OwnershipPolicy
		};
	}

	private int CountSunDrops()
	{
		int num = 0;
		foreach (Node item in GetTree().GetNodesInGroup("SunDropItem"))
		{
			if (item is TowerDefenseSunBase towerDefenseSunBase && GodotObject.IsInstanceValid(towerDefenseSunBase) && !towerDefenseSunBase.die && !towerDefenseSunBase.isCollect)
			{
				num++;
			}
		}
		return num;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[OnlineSunBomb:" + _role + "] " + message);
		}
	}
}
