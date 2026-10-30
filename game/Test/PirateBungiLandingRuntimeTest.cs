using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class PirateBungiLandingRuntimeTest : Node
{
	private static readonly Color BackgroundColor = new Color(0.015f, 0.015f, 0.015f);

	private const string CaptainScenePath = "res://Asset/Anime/Character/Zombie/Chapter7/Captain/Scene/TowerDefenseZombieCaptain.tscn";

	private const string CaptainPacketPath = "res://Asset/Anime/Character/Zombie/Chapter7/Captain/Packet/ZombieCaptain.tres";

	private const string CrewScenePath = "res://Asset/Anime/Character/Zombie/Chapter7/Crew/Scene/TowerDefenseZombieCrew.tscn";

	private const string CrewPacketPath = "res://Asset/Anime/Character/Zombie/Chapter7/Crew/Packet/ZombieCrew.tres";

	private const string CaptainShovelPath = "res://Asset/Config/Shovel/Config/ShovelCaptain.tres";

	private const int GroundFlags = 1;

	private int _checks;

	private int _failures;

	private Node2D _characterNode;

	private TowerDefenseZombieCaptain _sourceCaptain;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew control = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 5;
			try
			{
				GameSaveManager.Instance.SetConfigValue("Backgrounder", true);
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				ColorRect node = new ColorRect
				{
					Color = BackgroundColor,
					Position = Vector2.Zero,
					Size = new Vector2(1080f, 600f),
					ZIndex = -4096,
					MouseFilter = Control.MouseFilterEnum.Ignore
				};
				AddChild(node, forceReadableName: false, InternalMode.Disabled);
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ObjectManager.Instance), "ObjectManager autoload must be available for the shovel's real coin path.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ObjectManager.Instance))
				{
					goto end_IL_0064;
				}
				TowerDefenseProjectileRegistry.Init();
				RegisterRealPacketFixtures();
				control = new TowerDefenseControlNew
				{
					isGameRunning = true
				};
				_characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				AddChild(_characterNode, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = _characterNode;
				manager.currentControl = control;
				manager.gridBeginPos = new Vector2(0f, 100f);
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
				{
					gridBeginPos = manager.gridBeginPos,
					gridSize = manager.gridSize,
					gridNum = manager.gridNum,
					plantOffset = 50.0
				};
				towerDefenseMapConfig.cellConfig.Add(new TowerDefenseCellConfig
				{
					pos = new Vector4I(1, 1, 9, 5)
				});
				for (int i = 1; i <= 5; i++)
				{
					towerDefenseMapConfig.lineUse.Add(i);
				}
				mapFeature = new TowerDefenseBattleFeatureMap
				{
					control = control,
					config = towerDefenseMapConfig,
					mapConfig = towerDefenseMapConfig,
					mapControl = new TowerDefenseMapControl(),
					groundRect = new Rect2(0f, 100f, 900f, 380f),
					rect = new Rect2(-1000f, -1000f, 4000f, 4000f)
				};
				mapFeature.mapControl.mapFeature = mapFeature;
				mapFeature.PlantGridInit();
				control.featureDictionary["Map"] = mapFeature;
				_sourceCaptain = InstantiateCaptain();
				Check(GodotObject.IsInstanceValid(_sourceCaptain), "The real Captain scene must instantiate as the shovel target and summon source.");
				if (!GodotObject.IsInstanceValid(_sourceCaptain))
				{
					goto end_IL_0064;
				}
				_sourceCaptain.inGame = false;
				_sourceCaptain.editorPreviewMode = false;
				_sourceCaptain.gridPos = new Vector2I(5, 3);
				_sourceCaptain.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(_sourceCaptain.gridPos);
				_characterNode.AddChild(_sourceCaptain, forceReadableName: false, InternalMode.Disabled);
				await WaitPhysicsFrames(3);
				_sourceCaptain.instance.canBeCollection = false;
				ShovelEventCaptainShovelConfig shovelEvent = LoadCaptainShovelEvent();
				Check(GodotObject.IsInstanceValid(shovelEvent), "The real ShovelCaptain resource must expose ShovelEventCaptainShovelConfig.");
				if (!GodotObject.IsInstanceValid(shovelEvent))
				{
					goto end_IL_0064;
				}
				await VerifyShovelLanding(shovelEvent, 100.0, "ZombieCrew", "low-cost shovel");
				await VerifyShovelLanding(shovelEvent, 500.0, "ZombieCaptain", "high-cost shovel");
				_sourceCaptain.Hypnoses(60.0);
				Check(_sourceCaptain.instance.hypnoses && _sourceCaptain.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT, "The real Captain summon source must be hypnotized before SpawnCrew.");
				await VerifyCaptainSpawnCrewLanding();
				await WaitForAllCarriersToFinish();
				goto end_IL_0041;
				end_IL_0064:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[PirateBungiLandingRuntimeTest] Unexpected exception: {value}");
				goto end_IL_0041;
			}
			return;
			end_IL_0041:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = null;
			}
			if (GodotObject.IsInstanceValid(_characterNode))
			{
				_characterNode.Free();
			}
			_characterNode = null;
			_sourceCaptain = null;
			mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(mapFeature?.mapControl))
			{
				mapFeature.mapControl.Free();
			}
			UnregisterRealPacketFixtures();
			if (GodotObject.IsInstanceValid(control))
			{
				control.Free();
			}
			for (int frame = 0; frame < 4; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
		}
		bool flag = _failures == 0;
		GD.Print($"PIRATE_BUNGI_LANDING_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyShovelLanding(ShovelEventCaptainShovelConfig shovelEvent, double cost, string expectedPayload, string label)
	{
		HashSet<ulong> existingCarriers = SnapshotCarrierIds();
		_sourceCaptain.cost = cost;
		shovelEvent.Execute(_sourceCaptain);
		List<TowerDefenseZombieBungiSpawn> list = FindNewCarriers(existingCarriers, expectedPayload);
		Check(list.Count == 1, $"The {label} path must create exactly one {expectedPayload} bungee carrier at cost {cost}; got {list.Count}.");
		if (list.Count != 0)
		{
			list[0].SetLogicalGlobalPosition(new Vector2(540f, 400f));
			await ValidateLandedPayload(await WaitForNaturalLanding(list[0], label), expectedPayload, label);
		}
	}

	private async Task VerifyCaptainSpawnCrewLanding()
	{
		HashSet<ulong> existingCarriers = SnapshotCarrierIds();
		_sourceCaptain.SpawnCrew();
		List<TowerDefenseZombieBungiSpawn> list = FindNewCarriers(existingCarriers, "ZombieCrew");
		Check(list.Count == 4, $"Captain SpawnCrew must create four real ZombieCrew bungee carriers; got {list.Count}.");
		if (list.Count != 0)
		{
			list[0].SetLogicalGlobalPosition(new Vector2(540f, 400f));
			await ValidateLandedPayload(await WaitForNaturalLanding(list[0], "Captain SpawnCrew"), "ZombieCrew", "Captain SpawnCrew");
		}
	}

	private async Task<TowerDefenseZombie> WaitForNaturalLanding(TowerDefenseZombieBungiSpawn carrier, string label)
	{
		TowerDefenseZombie payload = null;
		bool observedDrop = false;
		bool payloadGravityRanDuringDrop = false;
		double maxDropZDelta = 0.0;
		float maxDropPositionDelta = 0f;
		int visibleDropSamples = 0;
		int blankDropSamples = 0;
		int minDropPixels = 2147483647;
		for (int frame = 0; frame < 240; frame++)
		{
			if (GodotObject.IsInstanceValid(carrier) && carrier.character is TowerDefenseZombie towerDefenseZombie)
			{
				payload = towerDefenseZombie;
			}
			if (GodotObject.IsInstanceValid(carrier) && GodotObject.IsInstanceValid(payload) && carrier.CurrentStateHandle?.StableId == "zombie.bungi_spawn.drop")
			{
				observedDrop = true;
				payloadGravityRanDuringDrop |= payload.gravityUse;
				maxDropZDelta = Math.Max(maxDropZDelta, Math.Abs(payload.z - carrier.z));
				maxDropPositionDelta = Math.Max(maxDropPositionDelta, payload.GetLogicalGlobalPosition().DistanceTo(carrier.GetLogicalGlobalPosition()));
				if (carrier.z <= 240.0 && carrier.z >= 20.0)
				{
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
					await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
					using Image image = GetViewport().GetTexture().GetImage();
					Vector2 logicalGlobalPosition = carrier.GetLogicalGlobalPosition(carrier.sprite);
					int num = CountForegroundPixels(image, (int)logicalGlobalPosition.X - 90, (int)logicalGlobalPosition.X + 90, (int)logicalGlobalPosition.Y - 120, (int)logicalGlobalPosition.Y + 100);
					visibleDropSamples++;
					minDropPixels = Math.Min(minDropPixels, num);
					if (num < 20)
					{
						blankDropSamples++;
					}
				}
			}
			if (GodotObject.IsInstanceValid(payload) && payload.GetParent() == _characterNode && carrier.CurrentStateHandle?.StableId == "zombie.bungi_spawn.rise")
			{
				Check(observedDrop, "The " + label + " fixture must observe the real synchronized drop interval.");
				Check(!payloadGravityRanDuringDrop, "The " + label + " payload gravity must remain suspended while the carrier owns its Z motion.");
				Check(maxDropZDelta <= 0.001, $"The {label} payload must share the carrier Z throughout descent; maxDelta={maxDropZDelta:F4}.");
				Check(maxDropPositionDelta <= 0.001f, $"The {label} payload must share the carrier position throughout descent; maxDelta={maxDropPositionDelta:F4}.");
				Check(visibleDropSamples >= 1 && blankDropSamples == 0, $"The {label} real bungee sprite must remain rendered throughout descent; samples={visibleDropSamples} blank={blankDropSamples} minPixels={minDropPixels}.");
				Check(condition: true, "The " + label + " payload completed the real Bungi drop and landing path.");
				return payload;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		Check(condition: false, "The " + label + " payload did not complete the real Bungi landing within 240 physics frames.");
		return payload;
	}

	private static int CountForegroundPixels(Image image, int x0, int x1, int y0, int y1)
	{
		int num = 0;
		int num2 = Math.Min(x1, image.GetWidth());
		int num3 = Math.Min(y1, image.GetHeight());
		for (int i = Math.Max(0, y0); i < num3; i += 2)
		{
			for (int j = Math.Max(0, x0); j < num2; j += 2)
			{
				Color pixel = image.GetPixel(j, i);
				if (Math.Abs(pixel.R - BackgroundColor.R) + Math.Abs(pixel.G - BackgroundColor.G) + Math.Abs(pixel.B - BackgroundColor.B) > 0.08f)
				{
					num++;
				}
			}
		}
		return num;
	}

	private async Task ValidateLandedPayload(TowerDefenseZombie payload, string expectedPayload, string label)
	{
		Check(GodotObject.IsInstanceValid(payload), "The " + label + " landing must retain a valid payload.");
		if (!GodotObject.IsInstanceValid(payload))
		{
			return;
		}
		Check(payload.config?.name == expectedPayload, $"The {label} payload must be {expectedPayload}; got {payload.config?.name ?? "<null>"}.");
		Check(payload.instance.hypnoses, "The " + label + " payload must inherit hypnosis through the real landing path.");
		Check(payload.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT, $"The hypnotized {label} payload must join the plant camp; got {payload.camp}.");
		PirateBungiLandingRuntimeTest pirateBungiLandingRuntimeTest = this;
		AttackComponent attackComponent = payload.attackComponent;
		pirateBungiLandingRuntimeTest.Check(attackComponent != null && !attackComponent.IsReleased && payload.attackComponent.alive, "The " + label + " payload attack runtime must be alive after landing.");
		Check(payload.instance.canBeCollection, "The " + label + " payload must re-enter target collection after landing.");
		Check(payload.instance.collisionFlags == 1, $"The {label} payload collision flags must be ground-only after landing; got {payload.instance.collisionFlags}.");
		Check(payload.instance.maskFlags == 1, $"The {label} payload mask flags must be ground-only after landing; got {payload.instance.maskFlags}.");
		Check(payload.gravityUse, "The " + label + " payload must restore its authored gravity behavior after landing.");
		Check(!payload.instance.invincible && !payload.instance.invincibleHurt && !payload.instance.invincibleSmash, "The " + label + " payload must not remain invincible after landing.");
		Check(!payload.die && !payload.nearDie && !payload.isDestroy && payload.instance.hitpoints > payload.instance.hitpointsNearDeath, "The " + label + " payload must be a live full character, not a dead/near-death half corpse.");
		for (int frame = 0; frame < 60; frame++)
		{
			if (!(payload.CurrentStateHandle?.StableId != "zombie.walk"))
			{
				GroundMoveComponent groundMoveComponent = payload.groundMoveComponent;
				if (groundMoveComponent != null && groundMoveComponent.Alive)
				{
					break;
				}
			}
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		Check(payload.CurrentStateHandle?.StableId == "zombie.walk", $"The {label} payload must enter zombie.walk after landing; got {payload.CurrentStateHandle?.StableId ?? "<null>"}.");
		PirateBungiLandingRuntimeTest pirateBungiLandingRuntimeTest2 = this;
		GroundMoveComponent groundMoveComponent2 = payload.groundMoveComponent;
		pirateBungiLandingRuntimeTest2.Check(groundMoveComponent2 != null && !groundMoveComponent2.IsReleased && groundMoveComponent2.Alive && payload.groundMoveComponent.HasMovementSource, "The " + label + " payload must restore an active ground movement runtime.");
		Vector2 beforeMove = payload.GetLogicalGlobalPosition();
		await WaitPhysicsFrames(30);
		float num = Mathf.Abs(payload.GetLogicalGlobalPosition().X - beforeMove.X);
		Check(num > 0.01f, $"The {label} payload must physically move after Walk; deltaX={num:F4}.");
	}

	private HashSet<ulong> SnapshotCarrierIds()
	{
		HashSet<ulong> hashSet = new HashSet<ulong>();
		foreach (Node child in _characterNode.GetChildren())
		{
			if (child is TowerDefenseZombieBungiSpawn towerDefenseZombieBungiSpawn)
			{
				hashSet.Add(towerDefenseZombieBungiSpawn.GetInstanceId());
			}
		}
		return hashSet;
	}

	private async Task WaitForAllCarriersToFinish()
	{
		for (int frame = 0; frame < 180; frame++)
		{
			bool flag = false;
			foreach (Node child in _characterNode.GetChildren())
			{
				if (child is TowerDefenseZombieBungiSpawn)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		Check(condition: false, "All real bungee carriers must finish their rise before fixture cleanup.");
	}

	private List<TowerDefenseZombieBungiSpawn> FindNewCarriers(HashSet<ulong> existingCarriers, string expectedPayload)
	{
		List<TowerDefenseZombieBungiSpawn> list = new List<TowerDefenseZombieBungiSpawn>();
		foreach (Node child in _characterNode.GetChildren())
		{
			if (child is TowerDefenseZombieBungiSpawn towerDefenseZombieBungiSpawn && !existingCarriers.Contains(towerDefenseZombieBungiSpawn.GetInstanceId()) && towerDefenseZombieBungiSpawn.characterName == expectedPayload)
			{
				list.Add(towerDefenseZombieBungiSpawn);
			}
		}
		return list;
	}

	private static TowerDefenseZombieCaptain InstantiateCaptain()
	{
		return ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter7/Captain/Scene/TowerDefenseZombieCaptain.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseZombieCaptain>(PackedScene.GenEditState.Disabled);
	}

	private static void RegisterRealPacketFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			instance.TOWERDEFENSE_PACKETS["ZombieCrew"] = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Zombie/Chapter7/Crew/Packet/ZombieCrew.tres", null, ResourceLoader.CacheMode.Ignore);
			instance.TOWERDEFENSE_PACKETS["ZombieCaptain"] = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Zombie/Chapter7/Captain/Packet/ZombieCaptain.tres", null, ResourceLoader.CacheMode.Ignore);
			instance.TOWERDEFENSE_CHARCATERS["ZombieCrew"] = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter7/Crew/Scene/TowerDefenseZombieCrew.tscn", null, ResourceLoader.CacheMode.Ignore);
			instance.TOWERDEFENSE_CHARCATERS["ZombieCaptain"] = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter7/Captain/Scene/TowerDefenseZombieCaptain.tscn", null, ResourceLoader.CacheMode.Ignore);
		}
	}

	private static void UnregisterRealPacketFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			instance.TOWERDEFENSE_PACKETS.Remove("ZombieCrew");
			instance.TOWERDEFENSE_PACKETS.Remove("ZombieCaptain");
			instance.TOWERDEFENSE_CHARCATERS.Remove("ZombieCrew");
			instance.TOWERDEFENSE_CHARCATERS.Remove("ZombieCaptain");
		}
	}

	private static ShovelEventCaptainShovelConfig LoadCaptainShovelEvent()
	{
		ShovelConfig shovelConfig = ResourceLoader.Load<ShovelConfig>("res://Asset/Config/Shovel/Config/ShovelCaptain.tres", null, ResourceLoader.CacheMode.Ignore);
		if (!GodotObject.IsInstanceValid(shovelConfig))
		{
			return null;
		}
		foreach (ShovelEventConfig @event in shovelConfig.eventList)
		{
			if (@event is ShovelEventCaptainShovelConfig result)
			{
				return result;
			}
		}
		return null;
	}

	private async Task WaitPhysicsFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[PirateBungiLandingRuntimeTest] " + message);
		}
	}
}
