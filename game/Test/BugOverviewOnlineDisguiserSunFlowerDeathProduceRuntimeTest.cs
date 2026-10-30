using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewOnlineDisguiserSunFlowerDeathProduceRuntimeTest : Node
{
	private const string PlantPacketPath = "res://Asset/Anime/Character/Plant/Star/DisguiserSunFlower/Packet/PlantDisguiserSunFlower.tres";

	private const string PlantScenePath = "res://Asset/Anime/Character/Plant/Star/DisguiserSunFlower/Scene/TowerDefensePlantDisguiserSunFlower.tscn";

	private const string ZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string OwnerPeerId = "disguiser-sunflower-owner";

	private const int PlantSyncId = 7251;

	private const int ZombieSyncId = 7252;

	private const int ExpectedProduceCount = 6;

	private const int ExpectedSunAmount = 25;

	private static readonly Vector2I TestGrid = new Vector2I(5, 3);

	private int _checks;

	private int _failures;

	private string _role = "";

	private int _port;

	private int _messageOrder;

	private int _firstOperationOrder = -1;

	private int _destroyOrder = -1;

	private int _componentOperationMessages;

	private int _destroyMessages;

	private int _clientReadyMessages;

	private bool _replayIgnored;

	private bool _hostDestroyObserved;

	private TowerDefenseBattleNetworkHost _networkHost;

	private TowerDefensePlantDisguiserSunFlower _plant;

	private TowerDefenseZombie _zombie;

	private ProduceComponent _produce;

	private readonly List<string> _operationSignatures = new List<string>();

	private readonly List<string> _actualDropSignatures = new List<string>();

	private readonly HashSet<ulong> _seenSunIds = new HashSet<ulong>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private string _resultSignature = "missing";

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		MultiPlayerManager multiplayer = MultiPlayerManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		OnlineDisguiserSunFlowerRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseBattleFeatureSun sunFeature = null;
		CharacterComponentDefinition delayedProduceDefinition = null;
		bool subscribed = false;
		bool destroySubscribed = false;
		try
		{
			_ = 7;
			try
			{
				ParseArguments();
				Check((_role == "host" || _role == "client") && _port > 0, "The fixture requires --role=host/client and a positive --port.");
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(multiplayer) && GodotObject.IsInstanceValid(ResourceManager.Instance) && GodotObject.IsInstanceValid(ObjectManager.Instance), "Required production autoloads must exist in both isolated processes.");
				if (_failures > 0)
				{
					return;
				}
				Check(RegisterRealFixtures(), "Both peers must temporarily register the real packet and character resources before initial sync.");
				if (_failures > 0)
				{
					return;
				}
				Check(StartTransport(multiplayer), (_role == "host") ? "CreateMatch must bind the requested real ENet port." : "JoinMatch must start a real ENet client.");
				if (_failures > 0)
				{
					return;
				}
				if (_role == "host")
				{
					GD.Print($"ONLINE_DISGUISER_SUNFLOWER_HOST_LISTENING port={_port}");
				}
				control = new OnlineDisguiserSunFlowerRuntimeControlStub
				{
					Name = "OnlineDisguiserSunFlowerRuntimeControl",
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
				Check(_networkHost != null && BindNetworkHost(control, _networkHost), "The production TowerDefenseBattleNetworkHost must be bound to the control.");
				if (_networkHost == null)
				{
					return;
				}
				multiplayer.OnMatchStateReceived += _networkHost.HandleLegacyMessage;
				multiplayer.OnMatchStateReceived += OnMatchStateReceived;
				subscribed = true;
				bool flag = await WaitUntil(() => multiplayer.IsConnect() && (_role == "client" || multiplayer.matchMembers.Count >= 2), 600);
				Check(flag, "The isolated host and client must establish a real ENet connection.");
				if (!flag)
				{
					return;
				}
				Check(manager.TryResolveTrustedPeerSunAccount("disguiser-sunflower-owner", out var accountId) && manager.TryGetSun(accountId, out var _), "Both peers must register the shared ledger through the production authenticated-peer resolver.");
				TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Star/DisguiserSunFlower/Packet/PlantDisguiserSunFlower.tres", null, ResourceLoader.CacheMode.Ignore);
				TowerDefensePacketConfig towerDefensePacketConfig2 = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(towerDefensePacketConfig) && towerDefensePacketConfig.saveKey == "PlantDisguiserSunFlower" && GodotObject.IsInstanceValid(towerDefensePacketConfig2) && towerDefensePacketConfig2.saveKey == "ZombieNormal", "The fixture must load the real reported plant and ZombieNormal packets.");
				_plant = Instantiate<TowerDefensePlantDisguiserSunFlower>("res://Asset/Anime/Character/Plant/Star/DisguiserSunFlower/Scene/TowerDefensePlantDisguiserSunFlower.tscn");
				_zombie = Instantiate<TowerDefenseZombie>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
				Check(GodotObject.IsInstanceValid(_plant) && _plant.config?.name == "PlantDisguiserSunFlower" && GodotObject.IsInstanceValid(_zombie) && _zombie.config?.name == "ZombieNormal", "Both processes must instantiate the two real production character scenes.");
				if (!GodotObject.IsInstanceValid(_plant) || !GodotObject.IsInstanceValid(_zombie))
				{
					return;
				}
				PrepareCharacter(_plant, accountId);
				PrepareCharacter(_zombie, accountId);
				control.characterNode.AddChild(_plant, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(_zombie, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(8);
				_zombie.ProcessMode = ProcessModeEnum.Disabled;
				_produce = _plant.componentManager?.GetRuntime<ProduceComponent>("character.produce");
				AttackComponent attack = _zombie.componentManager?.GetRuntime<AttackComponent>("character.attack.0");
				BugOverviewOnlineDisguiserSunFlowerDeathProduceRuntimeTest bugOverviewOnlineDisguiserSunFlowerDeathProduceRuntimeTest = this;
				ProduceComponent produce = _produce;
				bugOverviewOnlineDisguiserSunFlowerDeathProduceRuntimeTest.Check(produce != null && !produce.IsReleased && _produce._IZMMode && _produce.num == 25 && _produce.healthProductionSegments == 6, "The real plant must expose its authored six-segment ProduceComponent.");
				Check(attack != null && !attack.IsReleased && string.Equals(attack.attackType, "Eat", StringComparison.OrdinalIgnoreCase), "ZombieNormal must expose its real resource-backed eating AttackComponent.");
				produce = _produce;
				if (produce == null || produce.IsReleased || attack == null || attack.IsReleased)
				{
					return;
				}
				control.RegisterSyncCharacter(7251, _plant);
				control.RegisterSyncCharacter(7252, _zombie);
				_plant.OnDestroy += OnPlantDestroyed;
				destroySubscribed = true;
				Check(_plant.syncId == 7251 && _zombie.syncId == 7252 && control._syncCharacters.TryGetValue(7251, out var value) && value == _plant, "Both peers must register identical real character sync IDs.");
				Check(CountSunDrops() == 0, "The isolated fixture must start without any pre-existing Sun drops.");
				if (_role == "client")
				{
					delayedProduceDefinition = _produce.ComponentDefinition;
					Check(delayedProduceDefinition != null && _plant.componentManager.RemoveRuntimeComponent(_produce) && _plant.componentManager.GetRuntime<ProduceComponent>("character.produce") == null, "The client must temporarily remove the real Produce runtime to exercise the operation/destroy readiness barrier.");
					_produce = null;
					multiplayer.SendClientReady();
					Check(condition: true, "The client must send the production CLIENT_READY handshake.");
					goto IL_0b13;
				}
				bool flag2 = await WaitUntil(() => _clientReadyMessages >= 1, 600);
				Check(flag2, "The host must receive the client's production CLIENT_READY handshake.");
				if (!flag2)
				{
					return;
				}
				double num = _produce.hpNextInterval;
				Check(num > 0.0 && _produce.healthProductionSegments == 6 && _plant.instance.hitpoints > 0.0, "The real plant must expose a positive six-segment bite threshold.");
				int num2 = 0;
				for (int num3 = 0; num3 < 6; num3++)
				{
					if (!GodotObject.IsInstanceValid(_plant))
					{
						break;
					}
					if (_plant.isDestroy)
					{
						break;
					}
					attack.target = _plant;
					attack.AttackExecute(num);
					num2++;
				}
				Check(num2 == 6 && _hostDestroyObserved && _plant.isDestroy, "Six real ZombieNormal bites must drive the real plant through normal destruction.");
				goto IL_0b13;
				IL_0e0a:
				await WaitFrames(4);
				ValidateResult(accountId, control);
				if (_role == "host")
				{
					await ValidateFullHealthRemovalDoesNotProduce(accountId, control);
				}
				if (_role == "client")
				{
					multiplayer.SendClientReady();
					await WaitFrames(12);
				}
				else
				{
					Check(await WaitUntil(() => _clientReadyMessages >= 2, 600), "The host must remain connected until the client finishes validation.");
				}
				goto end_IL_00ff;
				IL_0b13:
				bool flag3 = await WaitUntil(() => _componentOperationMessages == 6 && _destroyMessages == 1, 600);
				Check(flag3, "All six produce operations and the one CHARACTER_DESTROY must complete on both peers.");
				if (!flag3)
				{
					return;
				}
				if (!(_role == "client"))
				{
					goto IL_0e0a;
				}
				Check(GodotObject.IsInstanceValid(_plant) && !_plant.isDestroy && control._syncCharacters.ContainsKey(7251), "CHARACTER_DESTROY must wait while earlier reliable Produce operations lack their runtime.");
				_produce = _plant.componentManager.AddRuntimeComponent(delayedProduceDefinition) as ProduceComponent;
				BugOverviewOnlineDisguiserSunFlowerDeathProduceRuntimeTest bugOverviewOnlineDisguiserSunFlowerDeathProduceRuntimeTest2 = this;
				produce = _produce;
				bugOverviewOnlineDisguiserSunFlowerDeathProduceRuntimeTest2.Check(produce != null && !produce.IsReleased && _produce._IZMMode, "The client must restore the real resource-backed Produce runtime before queued operations retry.");
				produce = _produce;
				if (produce == null || produce.IsReleased)
				{
					return;
				}
				Dictionary data = new Dictionary
				{
					["kind"] = "Unsupported",
					["amount"] = 25,
					["economy_owner"] = accountId.ToString()
				};
				_produce.ApplyNetworkOperation("produce", 1L, data);
				Check(CountSunDrops() == 0 && _plant.EconomyOwnerAccountId == accountId, "An invalid sequence-one operation must neither create Sun nor poison the real owner before the valid sequence-one retry.");
				for (int num4 = 0; num4 < 10; num4++)
				{
					if (CountSunDrops() >= 6)
					{
						break;
					}
					_networkHost?.Process(1.0 / 60.0);
				}
				Check(CountSunDrops() == 6 && control._syncCharacters.ContainsKey(7251) && !_plant.isDestroy, "Restoring the runtime must apply all six queued operations while the deferred destroy still preserves their owner.");
				_actualDropSignatures.Clear();
				foreach (TowerDefenseSunBase liveSun in GetLiveSuns())
				{
					_actualDropSignatures.Add(BuildDropSignature(liveSun));
				}
				_replayIgnored = CountSunDrops() == 6;
				_networkHost?.Process(1.0 / 60.0);
				Check(!control._syncCharacters.ContainsKey(7251) && _plant.isDestroy, "The deferred destroy must converge immediately after every earlier operation leaves the barrier.");
				goto IL_0e0a;
			}
			catch (Exception value2)
			{
				_failures++;
				GD.PushError($"[OnlineDisguiserSunFlower:{_role}] Unexpected exception: {value2}");
				goto end_IL_00ff;
			}
			end_IL_00ff:;
		}
		finally
		{
			if (destroySubscribed && GodotObject.IsInstanceValid(_plant))
			{
				_plant.OnDestroy -= OnPlantDestroyed;
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
			RestoreRealFixtures();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			ObjectManager.Instance?.Clear();
			ResourceManager.Instance?.ReleaseTransientResources();
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			await WaitFramesWithoutNetwork(16);
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag4 = _failures == 0;
		string value3 = ((_role == "host") ? "ONLINE_DISGUISER_SUNFLOWER_HOST_RESULT" : "ONLINE_DISGUISER_SUNFLOWER_CLIENT_RESULT");
		GD.Print($"{value3} passed={flag4} checks={_checks} failures={_failures} operations={_componentOperationMessages} destroy_messages={_destroyMessages} sun_count={_actualDropSignatures.Count} op_before_destroy={_firstOperationOrder >= 0 && _firstOperationOrder < _destroyOrder} replay_ignored={_replayIgnored} signature={_resultSignature}");
		GetTree().Quit((!flag4) ? 2 : 0);
	}

	private async Task ValidateFullHealthRemovalDoesNotProduce(EconomyAccountId accountId, OnlineDisguiserSunFlowerRuntimeControlStub control)
	{
		int beforeRemoval = CountSunDrops();
		TowerDefensePlantDisguiserSunFlower removalPlant = Instantiate<TowerDefensePlantDisguiserSunFlower>("res://Asset/Anime/Character/Plant/Star/DisguiserSunFlower/Scene/TowerDefensePlantDisguiserSunFlower.tscn");
		Check(GodotObject.IsInstanceValid(removalPlant), "The full-health removal negative case must use a second real DisguiserSunFlower.");
		if (GodotObject.IsInstanceValid(removalPlant))
		{
			PrepareCharacter(removalPlant, accountId);
			removalPlant.gridPos = new Vector2I(TestGrid.X - 1, TestGrid.Y);
			removalPlant.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(removalPlant.gridPos);
			control.characterNode.AddChild(removalPlant, forceReadableName: false, InternalMode.Disabled);
			await WaitFrames(4);
			ProduceComponent produceComponent = removalPlant.componentManager?.GetRuntime<ProduceComponent>("character.produce");
			Check(produceComponent != null && !produceComponent.IsReleased && removalPlant.instance.hitpoints > 0.0 && !removalPlant.die, "The negative case must remove a live full-health real plant with an active Produce runtime.");
			removalPlant.Destroy(freeInstance: false);
			await WaitFrames(6);
			Check(CountSunDrops() == beforeRemoval, "A full-health ordinary removal must create zero IZM death-production Sun drops.");
			if (GodotObject.IsInstanceValid(removalPlant) && !removalPlant.IsQueuedForDeletion())
			{
				removalPlant.QueueFree();
			}
		}
	}

	private void ValidateResult(EconomyAccountId accountId, OnlineDisguiserSunFlowerRuntimeControlStub control)
	{
		Check(_componentOperationMessages == 6, "Exactly six reliable produce operations must be authored/received.");
		Check(_destroyMessages == 1 && _firstOperationOrder >= 0 && _destroyOrder > _firstOperationOrder && _destroyOrder == 7, "The single CHARACTER_DESTROY must follow all six reliable operations.");
		Check(_actualDropSignatures.Count == 6 && CountSunDrops() == 6, "Both peers must retain exactly six real live Sun nodes.");
		Check(AllDropsHaveExpectedEconomy(accountId), "Every drop must be exactly 25 Sun and owned by the same registered economy account.");
		if (_role == "client")
		{
			_operationSignatures.Sort(StringComparer.Ordinal);
			_actualDropSignatures.Sort(StringComparer.Ordinal);
			Check(_operationSignatures.Count == 6 && string.Join("\n", _operationSignatures) == string.Join("\n", _actualDropSignatures), "Every client Sun node must exactly match its host-authored operation fields.");
			Check(_replayIgnored, "Replaying the sixth sequence through the production network host must create no seventh Sun.");
		}
		else
		{
			_replayIgnored = true;
		}
		Check(!control._syncCharacters.ContainsKey(7251), "The plant may leave synchronized state only after all production operations are sent.");
		_resultSignature = EncodeSignature(_actualDropSignatures);
	}

	private void OnMatchStateReceived(string opCode, string data, string senderId)
	{
		if (opCode == "client_ready" && _role == "host")
		{
			_clientReadyMessages++;
		}
		else
		{
			if (_role != "client")
			{
				return;
			}
			if (opCode == "character_component_operation")
			{
				Variant variant = Json.ParseString(data);
				if (variant.VariantType != Variant.Type.Dictionary)
				{
					return;
				}
				Dictionary dictionary = variant.AsGodotDictionary();
				if (dictionary.GetValueOrDefault("sync_id", -1).AsInt32() == 7251 && !(dictionary.GetValueOrDefault("component_instance_id", "").AsString() != "character.produce") && !(dictionary.GetValueOrDefault("component_type_id", "").AsString() != "ProduceComponent") && !(dictionary.GetValueOrDefault("operation_name", "").AsString() != "produce"))
				{
					Dictionary operation = dictionary.GetValueOrDefault("data", new Dictionary()).AsGodotDictionary();
					_messageOrder++;
					_componentOperationMessages++;
					if (_firstOperationOrder < 0)
					{
						_firstOperationOrder = _messageOrder;
					}
					_operationSignatures.Add(BuildOperationSignature(operation));
					TowerDefenseSunBase towerDefenseSunBase = CaptureNewSun();
					if (GodotObject.IsInstanceValid(towerDefenseSunBase))
					{
						_actualDropSignatures.Add(BuildDropSignature(towerDefenseSunBase));
					}
					if (_componentOperationMessages == 6)
					{
						int num = CountSunDrops();
						_networkHost?.HandleLegacyMessage(opCode, data, senderId);
						_replayIgnored = CountSunDrops() == num;
					}
				}
			}
			else if (opCode == "character_destroy")
			{
				CharacterDestroyDto characterDestroyDto = MatchStateSerializer.Deserialize<CharacterDestroyDto>(data);
				if (characterDestroyDto != null && characterDestroyDto.sync_id == 7251)
				{
					_messageOrder++;
					_destroyMessages++;
					_destroyOrder = _messageOrder;
				}
			}
		}
	}

	private void OnPlantDestroyed(TowerDefenseCharacter character)
	{
		if (_role != "host" || character != _plant)
		{
			return;
		}
		_hostDestroyObserved = true;
		_actualDropSignatures.Clear();
		foreach (TowerDefenseSunBase liveSun in GetLiveSuns())
		{
			_actualDropSignatures.Add(BuildDropSignature(liveSun));
		}
		_componentOperationMessages = _actualDropSignatures.Count;
		_firstOperationOrder = ((_componentOperationMessages > 0) ? 1 : (-1));
		_destroyMessages = 1;
		_destroyOrder = _componentOperationMessages + 1;
	}

	private static void PrepareCharacter(TowerDefenseCharacter character, EconomyAccountId accountId)
	{
		character.inGame = true;
		character.editorPreviewMode = false;
		character.gridPos = TestGrid;
		character.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(TestGrid);
		character.TryAssignEconomyOwner(accountId);
	}

	private TowerDefenseSunBase CaptureNewSun()
	{
		foreach (TowerDefenseSunBase liveSun in GetLiveSuns())
		{
			ulong instanceId = liveSun.GetInstanceId();
			if (_seenSunIds.Add(instanceId))
			{
				return liveSun;
			}
		}
		return null;
	}

	private List<TowerDefenseSunBase> GetLiveSuns()
	{
		List<TowerDefenseSunBase> list = new List<TowerDefenseSunBase>();
		foreach (Node item in GetTree().GetNodesInGroup("SunDropItem"))
		{
			if (item is TowerDefenseSunBase towerDefenseSunBase && GodotObject.IsInstanceValid(towerDefenseSunBase) && !towerDefenseSunBase.die && !towerDefenseSunBase.isCollect)
			{
				list.Add(towerDefenseSunBase);
			}
		}
		return list;
	}

	private int CountSunDrops()
	{
		return GetLiveSuns().Count;
	}

	private bool AllDropsHaveExpectedEconomy(EconomyAccountId accountId)
	{
		foreach (TowerDefenseSunBase liveSun in GetLiveSuns())
		{
			if (liveSun.sunNum != 25 || liveSun.AccountId != accountId || liveSun.OwnershipPolicy != SunDropOwnershipPolicy.AccountOwned || liveSun.movingMethod != TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY)
			{
				return false;
			}
		}
		return CountSunDrops() == 6;
	}

	private static string BuildOperationSignature(Dictionary operation)
	{
		_003C_003Ey__InlineArray9<object> buffer = default(_003C_003Ey__InlineArray9<object>);
		buffer[0] = operation.GetValueOrDefault("kind", "").AsString();
		buffer[1] = operation.GetValueOrDefault("amount", 0).AsInt32();
		buffer[2] = F("position_x");
		buffer[3] = F("position_y");
		buffer[4] = F("velocity_x");
		buffer[5] = F("velocity_y");
		buffer[6] = F("gravity");
		buffer[7] = operation.GetValueOrDefault("economy_owner", "").AsString();
		buffer[8] = 1;
		return string.Join("|", (ReadOnlySpan<object?>)buffer);
		string F(string key)
		{
			return operation.GetValueOrDefault(key, 0f).AsSingle().ToString("R", CultureInfo.InvariantCulture);
		}
	}

	private static string BuildDropSignature(TowerDefenseSunBase sun)
	{
		_003C_003Ey__InlineArray9<object> buffer = default(_003C_003Ey__InlineArray9<object>);
		buffer[0] = "Sun";
		buffer[1] = sun.sunNum;
		buffer[2] = F(sun.GlobalPosition.X);
		buffer[3] = F(sun.GlobalPosition.Y);
		buffer[4] = F(sun.moveComponent?.velocity.X ?? 0f);
		buffer[5] = F(sun.moveComponent?.velocity.Y ?? 0f);
		buffer[6] = F((float)(sun.moveComponent?.gravity ?? 0.0));
		buffer[7] = sun.AccountId.ToString();
		buffer[8] = (int)sun.OwnershipPolicy;
		return string.Join("|", (ReadOnlySpan<object?>)buffer);
		static string F(float value)
		{
			return value.ToString("R", CultureInfo.InvariantCulture);
		}
	}

	private static string EncodeSignature(List<string> signatures)
	{
		List<string> list = new List<string>(signatures);
		list.Sort(StringComparer.Ordinal);
		return Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join("\n", list)));
	}

	private bool RegisterRealFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		RegisterPacket(instance, "PlantDisguiserSunFlower", "res://Asset/Anime/Character/Plant/Star/DisguiserSunFlower/Packet/PlantDisguiserSunFlower.tres");
		RegisterCharacter(instance, "PlantDisguiserSunFlower", "res://Asset/Anime/Character/Plant/Star/DisguiserSunFlower/Scene/TowerDefensePlantDisguiserSunFlower.tscn");
		RegisterPacket(instance, "ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
		RegisterCharacter(instance, "ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
		if (instance.TOWERDEFENSE_PACKETS["PlantDisguiserSunFlower"] is TowerDefensePacketConfig && instance.TOWERDEFENSE_CHARCATERS["PlantDisguiserSunFlower"] is PackedScene && instance.TOWERDEFENSE_PACKETS["ZombieNormal"] is TowerDefensePacketConfig)
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

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[OnlineDisguiserSunFlower:" + _role + "] " + message);
		}
	}
}
