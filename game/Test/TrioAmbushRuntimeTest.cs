using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class TrioAmbushRuntimeTest : Node
{
	private int _checks;

	private TrioAmbushTestControl _control;

	private TowerDefenseBattleFeatureWave _wave;

	private void Check(bool condition, string label)
	{
		_checks++;
		if (!condition)
		{
			throw new InvalidOperationException(label);
		}
	}

	private async Task Frames(int count)
	{
		for (int i = 0; i < count; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private TowerDefenseZombie[] Payloads()
	{
		return (from z in _control.characterNode.GetChildren().OfType<TowerDefenseZombie>()
			where !(z is TowerDefenseTrioBungi) && !z.IsQueuedForDeletion()
			select z).ToArray();
	}

	public override async void _Ready()
	{
		bool passed = false;
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previous = manager.currentControl;
		Vector2 previousBegin = manager.gridBeginPos;
		Vector2 previousSize = manager.gridSize;
		Vector2I previousNum = manager.gridNum;
		try
		{
			RuleChecks();
			Setup(manager);
			await Frames(3);
			TrioAmbushRuntime runtime = _wave.TrioRuntime;
			runtime.SetPhysicsProcess(enable: false);
			TowerDefenseLevelEventRegistry.Create("CoralTrioSpawn").Execute();
			Check(_wave.HasPendingSpawnOperations, "The event immediately reserves the wave against premature settlement.");
			_control.isGameRunning = false;
			runtime._PhysicsProcess(10.0);
			Check(runtime.Clock.Time < 1.0 && Payloads().Length == 0, "Pausing the battle does not advance the scheduled event.");
			_control.isGameRunning = true;
			runtime._PhysicsProcess(1.99);
			Check(Payloads().Length == 0, "No coral before 200cs.");
			BattleEventBus.Instance.EmitColdEffectEmit();
			runtime._PhysicsProcess(0.01);
			Check(Payloads().Length == 0 && !_wave.HasPendingSpawnOperations, "Ice cancels the entire pending group.");
			runtime._PhysicsProcess(3.0);
			Check(Payloads().Length == 0, "Thaw never respawns a cancelled group.");
			TowerDefenseLevelEventRegistry.Create("CoralTrioSpawn").Execute();
			runtime._PhysicsProcess(2.0);
			await Frames(3);
			Check(Payloads().Length == 3 && _wave.HasPendingSpawnOperations, $"Three real coral payloads reserve their emergence. count={Payloads().Length} pending={_wave.HasPendingSpawnOperations} ice={runtime.Clock.IceRemaining} running={_control.isGameRunning} children={string.Join(',', from n in _control.characterNode.GetChildren()
				select n.Name)}");
			Check(Payloads().All((TowerDefenseZombie z) => z.inWater && z.gridPos.X >= 6 && z.gridPos.X <= 9), "Coral only uses the right four water columns.");
			Check((from z in Payloads()
				select z.gridPos).Distinct().Count() == 3, "Coral cells never repeat.");
			Check(Payloads().All((TowerDefenseZombie z) => z.isRise && z.z < 0.0 - z.waterHeight), "Coral remains below the water surface during emergence; vertical physics must not snap it up.");
			await Frames(45);
			Check(!_wave.HasPendingSpawnOperations, "The emergence operation settles.");
			Check(Payloads().All((TowerDefenseZombie z) => !z.isRise && z.HasNode("TrioSeaweed") && z.attackComponent.alive), "Coral exits into normal combat with attached seaweed.");
			TowerDefenseZombie character = Payloads()[0];
			TowerDefenseCharacterSaveConfigCSharp towerDefenseCharacterSaveConfigCSharp = new TowerDefenseCharacterSaveConfigCSharp();
			towerDefenseCharacterSaveConfigCSharp.SaveCharacter(character);
			Check(towerDefenseCharacterSaveConfigCSharp.characterFlags.GetValueOrDefault("trio_coral", false).AsBool(), "Stable saves retain the coral appearance marker.");
			TowerDefenseCharacter restored = towerDefenseCharacterSaveConfigCSharp.LoadCharacter();
			await Frames(3);
			Check(restored.HasNode("TrioSeaweed") && !restored.isRise, "A real restored coral retains its decorations without replaying emergence.");
			restored.Destroy();
			TowerDefenseZombie[] array = Payloads();
			for (int num = 0; num < array.Length; num++)
			{
				array[num].Destroy();
			}
			await Frames(5);
			TowerDefenseLevelEventRegistry.Create("CoralTrioSpawn").Execute();
			runtime._PhysicsProcess(2.0);
			await Frames(7);
			TowerDefenseZombie emerging = Payloads()[0];
			Check(emerging.isRise, "The explosion probe targets the actual emergence phase.");
			TowerDefenseCharacter cherry = TowerDefenseManager.GetPacketConfig("PlantCherryBomb").Plant(emerging.gridPos, playAudio: false, noLimit: true, default(EconomyAccountId), skipPlacementCheck: true);
			await Frames(2);
			Check(GodotObject.IsInstanceValid(cherry), "The real cherry bomb fixture must enter the scene.");
			ExplodeComponent runtime2 = cherry.componentManager.GetRuntime<ExplodeComponent>("character.explode");
			Check(runtime2 != null, "The real cherry bomb explosion component must be bound.");
			runtime2.Explode();
			await Frames(5);
			Check(!GodotObject.IsInstanceValid(emerging) || emerging.die || emerging.nearDie || emerging.isDestroy, "The real cherry bomb explosion can kill an emerging coral zombie.");
			array = Payloads();
			for (int num = 0; num < array.Length; num++)
			{
				array[num].Destroy();
			}
			if (GodotObject.IsInstanceValid(cherry) && !cherry.isDestroy)
			{
				cherry.Destroy();
			}
			await Frames(5);
			TowerDefenseLevelEventRegistry.Create("BungiTrioSpawn").Execute();
			runtime._PhysicsProcess(2.0);
			await Frames(5);
			Check(Payloads().Length == 3, "Three real air-drop payloads.");
			Check(Payloads().All((TowerDefenseZombie z) => !z.isGround && !z.instance.canBeCollection), "Carried payloads cannot attack or enter ordinary target collection.");
			Check(Payloads().All((TowerDefenseZombie z) => !z.sprite.GetFliter("Zombie_duckytube")), "Air-drop payloads must not show swimming rings in the sky.");
			Check(_control.characterNode.GetChildren().OfType<TowerDefenseTrioBungi>().Count() == 3, "Each payload has its own real umbrella-blockable carrier.");
			BattleEventBus.Instance.EmitColdEffectEmit();
			Check(Payloads().All((TowerDefenseZombie z) => z.buff.buffDictionary.ContainsKey("Frozen")), "Already spawned air-drop payloads freeze in flight.");
			Check(!_wave.CanSaveProgress(out var _), "Progress cannot save during an air-drop.");
			await Frames(315);
			Check(!_wave.HasPendingSpawnOperations, "Air-drop and carrier departure release all wave reservations.");
			Check(Payloads().Length == 3 && Payloads().All((TowerDefenseZombie z) => z.isGround && z.instance.canBeCollection), "Frozen payloads still arrive; ice is not a retroactive cancellation.");
			Check(Payloads().All((TowerDefenseZombie z) => z.sprite.GetFliter("Zombie_duckytube") == z.inWater), "Landing restores the water visuals according to the actual terrain.");
			array = Payloads();
			for (int num = 0; num < array.Length; num++)
			{
				array[num].Destroy();
			}
			await Frames(5);
			TowerDefenseCharacter umbrella = TowerDefenseManager.GetPacketConfig("PlantUmbrellaleaf").Plant(new Vector2I(7, 2), playAudio: false);
			await Frames(4);
			TowerDefenseZombie payload = TowerDefenseManager.GetPacketConfig("ZombieNormal").Plant(new Vector2I(7, 2), playAudio: false) as TowerDefenseZombie;
			int operation = _wave.BeginPendingSpawnOperation();
			TrioAmbushMember.Attach(payload, coral: false, 45.0, _wave, operation);
			await Frames(80);
			Check(!GodotObject.IsInstanceValid(payload) || payload.isDestroy, "The real umbrella repels the payload rather than placing it.");
			Check(!_wave.HasPendingSpawnOperations, "Umbrella cancellation releases the operation.");
			umbrella.Destroy();
			TowerDefenseBattleNetworkContext context = new TowerDefenseBattleNetworkContext(_control);
			BattleEventReplicator replica = new BattleEventReplicator(context, context);
			try
			{
				int beforeReplica = Payloads().Length;
				Dictionary message = new Dictionary
				{
					["packet_name"] = "ZombieNormal",
					["grid_x"] = 6,
					["grid_y"] = 3,
					["sync_id"] = 51001,
					["spawn_state"] = new Dictionary { ["trio_ambush"] = new Dictionary
					{
						["coral"] = true,
						["phase"] = 4,
						["height"] = 0,
						["elapsed"] = 0.5
					} }
				};
				replica.ApplySpawnCharacterAt(message);
				replica.ApplySpawnCharacterAt(message);
				await Frames(4);
				Check(Payloads().Length == beforeReplica + 1, "Duplicated spawn packets create only one replica.");
				TowerDefenseCharacter towerDefenseCharacter = context.SyncCharacters[51001];
				Check(towerDefenseCharacter.HasNode("TrioSeaweed"), "The actual replication path restores coral visuals.");
				towerDefenseCharacter.Destroy();
				await Frames(4);
				replica.ApplySpawnCharacterAt(message);
				await Frames(3);
				Check(Payloads().Length == beforeReplica, "A late duplicate never resurrects a destroyed replica.");
			}
			finally
			{
				replica.Dispose();
			}
			TowerDefenseLevelEventRegistry.Create("BungiTrioSpawn").Execute();
			TowerDefenseLevelEventRegistry.Create("BungiTrioSpawn").Execute();
			Check(_wave.HasPendingSpawnOperations, "Multiple events reserve independently.");
			_wave.Destroy();
			Check(!_wave.HasPendingSpawnOperations, "Battle destruction cancels pending groups.");
			passed = true;
		}
		catch (Exception value)
		{
			GD.PushError($"TRIO_AMBUSH_FAILURE {value}");
		}
		finally
		{
			_wave?.Destroy();
			if (GodotObject.IsInstanceValid(_control))
			{
				TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = _control.GetFeature("Map") as TowerDefenseBattleFeatureMap;
				if (GodotObject.IsInstanceValid(towerDefenseBattleFeatureMap?.mapControl))
				{
					towerDefenseBattleFeatureMap.mapControl.Free();
				}
				if (GodotObject.IsInstanceValid(_control.levelControl))
				{
					_control.levelControl.Free();
				}
				_control.QueueFree();
			}
			manager.currentControl = previous;
			manager.gridBeginPos = previousBegin;
			manager.gridSize = previousSize;
			manager.gridNum = previousNum;
		}
		GD.Print($"TRIO_AMBUSH_RESULT passed={passed} checks={_checks}");
		GetTree().Quit((!passed) ? 2 : 0);
	}

	private void RuleChecks()
	{
		string[] array = new string[2] { "CoralTrioSpawn", "BungiTrioSpawn" };
		foreach (string text in array)
		{
			TowerDefenseLevelEventBase towerDefenseLevelEventBase = TowerDefenseLevelEventRegistry.Create(text);
			Check(towerDefenseLevelEventBase != null, "Registered event: " + text);
			towerDefenseLevelEventBase.Init(new Dictionary());
			Dictionary dictionary = Json.ParseString(Json.Stringify(towerDefenseLevelEventBase.Export())).AsGodotDictionary();
			Check(dictionary["EventName"].AsString() == text && towerDefenseLevelEventBase.GetProperty().Count == 0, "Parameterless round-trip: " + text);
		}
		Check(TrioAmbushRules.PickZombie(0) == "ZombieNormal" && TrioAmbushRules.PickZombie(3999) == "ZombieNormal", "Normal 4000 slots.");
		Check(TrioAmbushRules.PickZombie(4000) == "ZombieNormalCone" && TrioAmbushRules.PickZombie(7999) == "ZombieNormalCone", "Cone 4000 slots.");
		Check(TrioAmbushRules.PickZombie(8000) == "ZombieNormalBucket" && TrioAmbushRules.PickZombie(10999) == "ZombieNormalBucket", "Bucket 3000 slots.");
		List<Vector2I> list = TrioAmbushRules.CandidateCells(new Vector2I(12, 7), coral: true, (Vector2I p) => p.Y == 2 || p.Y == 6);
		Check(list.Count == 8 && list.All((Vector2I p) => p.X >= 9), "Nonstandard map rightmost four water columns.");
		Check(TrioAmbushRules.PickCells(list, coral: true, (int _) => 0).Distinct().Count() == 3, "Coral removes each selected cell.");
		Check(TrioAmbushRules.PickCells(list, coral: false, (int _) => 0).Distinct().Count() == 1, "Bungee retains a positive repeat weight.");
		Check(TrioAmbushRules.PickCells(list.Take(2).ToList(), coral: false, (int _) => 0).Count == 2, "Too few valid cells reduces count.");
		Check(TrioAmbushRules.PickCells(System.Array.Empty<Vector2I>(), coral: true, (int _) => 0).Count == 0, "No water ends cleanly.");
		TrioAmbushClock trioAmbushClock = new TrioAmbushClock();
		trioAmbushClock.Freeze();
		trioAmbushClock.Advance(2.99);
		Check(trioAmbushClock.Frozen, "Ice includes its last 1cs.");
		trioAmbushClock.Advance(0.01);
		Check(!trioAmbushClock.Frozen, "Ice excludes its exact expiry.");
		trioAmbushClock.Freeze();
		trioAmbushClock.Advance(2.0);
		trioAmbushClock.Freeze();
		trioAmbushClock.Advance(2.99);
		Check(trioAmbushClock.Frozen && Math.Abs(trioAmbushClock.IceRemaining - 0.01) < 1E-08, "Repeated ice resets instead of accumulating.");
		trioAmbushClock.RestoreIce(1.25);
		trioAmbushClock.Advance(1.25);
		Check(!trioAmbushClock.Frozen, "Save round-trip preserves remaining window.");
	}

	private void Setup(TowerDefenseManager manager)
	{
		_control = new TrioAmbushTestControl
		{
			Name = "TrioTestBattle",
			isInit = true,
			isGameRunning = true,
			levelConfig = new TowerDefenseLevelConfig()
		};
		AddChild(_control, forceReadableName: false, InternalMode.Disabled);
		_control.characterNode = new Node2D
		{
			Name = "CharacterNode"
		};
		_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
		_control.levelControl = new TowerDefenseInGameLevelControl
		{
			awardCreate = false
		};
		manager.currentControl = _control;
		manager.gridBeginPos = new Vector2(50f, 50f);
		manager.gridSize = new Vector2(80f, 85f);
		manager.gridNum = new Vector2I(9, 6);
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			gridNum = manager.gridNum,
			gridSize = manager.gridSize,
			gridBeginPos = manager.gridBeginPos
		};
		TowerDefenseMapControl towerDefenseMapControl = new TowerDefenseMapControl();
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (towerDefenseMapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			control = _control,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig,
			mapControl = towerDefenseMapControl
		});
		towerDefenseMapConfig.cellConfig = new Array<TowerDefenseCellConfig>
		{
			new TowerDefenseCellConfig
			{
				pos = new Vector4I(1, 1, 9, 6),
				gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
				{
					TowerDefenseEnum.PLANTGRIDTYPE.GROUND,
					TowerDefenseEnum.PLANTGRIDTYPE.AIR,
					TowerDefenseEnum.PLANTGRIDTYPE.SOIL
				}
			},
			new TowerDefenseCellConfig
			{
				pos = new Vector4I(1, 3, 9, 4),
				gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
				{
					TowerDefenseEnum.PLANTGRIDTYPE.GROUND,
					TowerDefenseEnum.PLANTGRIDTYPE.AIR,
					TowerDefenseEnum.PLANTGRIDTYPE.WATER
				}
			}
		};
		towerDefenseBattleFeatureMap.PlantGridInit();
		for (int i = 1; i <= 6; i++)
		{
			towerDefenseBattleFeatureMap.SetLineUse(i, use: true);
		}
		_control.featureDictionary["Map"] = towerDefenseBattleFeatureMap;
		_wave = new TowerDefenseBattleFeatureWave
		{
			control = _control,
			levelControl = _control.levelControl,
			mapFeature = towerDefenseBattleFeatureMap,
			config = new TowerDefenseLevelWaveManagerConfig()
		};
		_control.featureDictionary["Wave"] = _wave;
		_wave.OnReady();
	}
}
