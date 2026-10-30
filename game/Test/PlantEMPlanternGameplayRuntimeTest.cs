using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class PlantEMPlanternGameplayRuntimeTest : Node
{
	private const string PlantScenePath = "res://Asset/Anime/Character/Plant/Chapter9/EMPlantern/Scene/TowerDefensePlantEMPlantern.tscn";

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string CurrentEffectScenePath = "res://Asset/Anime/Character/Plant/Chapter9/EMPlantern/Effect/Current/EMPlanternCurrent.tscn";

	private readonly Vector2I _sourceGrid = new Vector2I(3, 3);

	private readonly Vector2I _targetGrid = new Vector2I(5, 3);

	private readonly Vector2I _zombieGrid = new Vector2I(4, 3);

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		PlantEMPlanternGameplayRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefensePlantEMPlantern source = null;
		TowerDefensePlantEMPlantern partner = null;
		TowerDefensePlantEMPlantern clientSource = null;
		TowerDefenseZombie normal = null;
		TowerDefenseZombie machine = null;
		TowerDefenseZombie metalArmor = null;
		TowerDefenseZombie bossMachine = null;
		try
		{
			_ = 5;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					goto end_IL_010b;
				}
				control = new PlantEMPlanternGameplayRuntimeControlStub
				{
					Name = "EMPlanternGameplayRuntimeControl",
					isGameRunning = false,
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
				manager.gridBeginPos = new Vector2(0f, 100f);
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				source = Instantiate<TowerDefensePlantEMPlantern>("res://Asset/Anime/Character/Plant/Chapter9/EMPlantern/Scene/TowerDefensePlantEMPlantern.tscn");
				partner = Instantiate<TowerDefensePlantEMPlantern>("res://Asset/Anime/Character/Plant/Chapter9/EMPlantern/Scene/TowerDefensePlantEMPlantern.tscn");
				normal = Instantiate<TowerDefenseZombie>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
				machine = Instantiate<TowerDefenseZombie>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
				metalArmor = Instantiate<TowerDefenseZombie>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
				bossMachine = Instantiate<TowerDefenseZombie>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
				Check(GodotObject.IsInstanceValid(source) && GodotObject.IsInstanceValid(partner) && GodotObject.IsInstanceValid(normal) && GodotObject.IsInstanceValid(machine) && GodotObject.IsInstanceValid(metalArmor) && GodotObject.IsInstanceValid(bossMachine), "The fixture must instantiate the production EM Plantern and zombie scenes.");
				if (!GodotObject.IsInstanceValid(source) || !GodotObject.IsInstanceValid(partner) || !GodotObject.IsInstanceValid(normal) || !GodotObject.IsInstanceValid(machine) || !GodotObject.IsInstanceValid(metalArmor) || !GodotObject.IsInstanceValid(bossMachine))
				{
					goto end_IL_010b;
				}
				PrepareCharacter(source, _sourceGrid, manager);
				PrepareCharacter(partner, _targetGrid, manager);
				PrepareCharacter(normal, _zombieGrid, manager);
				PrepareCharacter(machine, _zombieGrid, manager);
				PrepareCharacter(metalArmor, _zombieGrid, manager);
				PrepareCharacter(bossMachine, _zombieGrid, manager);
				control.characterNode.AddChild(source, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(normal, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(machine, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(metalArmor, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(bossMachine, forceReadableName: false, InternalMode.Disabled);
				AddToCell(source, _sourceGrid);
				await WaitFrames(4);
				normal.instance.hitpoints = (normal.instance.hitpointsBase = 10000.0);
				machine.instance.hitpoints = (machine.instance.hitpointsBase = 10000.0);
				metalArmor.instance.hitpoints = (metalArmor.instance.hitpointsBase = 10000.0);
				bossMachine.instance.hitpoints = (bossMachine.instance.hitpointsBase = 10000.0);
				machine.instance.physiqueTypeFlags |= 2048;
				metalArmor.instance.armorList.Add(new TowerDefenseArmorInstance
				{
					armorMethodFlags = 16,
					hitPoints = 100.0,
					hitpointsSave = 100.0
				});
				bossMachine.instance.physiqueTypeFlags |= 2048;
				bossMachine.instance.zombiePhysique = TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS;
				control.isGameRunning = true;
				Check(source.inGame && !source.die && !source.nearDie && manager.IsGameRunning(), "The fixture source must be an active in-game character.");
				Check(TowerDefenseManager.HasGameplayAuthority, "The isolated gameplay fixture must own gameplay authority.");
				Check(source.CanTarget(normal) && source.CanCollision(normal.instance.maskFlags), "The production target filter must accept the normal zombie fixture.");
				double hitpoints = normal.instance.hitpoints;
				double hitpoints2 = machine.instance.hitpoints;
				double hitpoints3 = metalArmor.instance.hitpoints;
				double hitpoints4 = bossMachine.instance.hitpoints;
				source.BatchUpdate(3.0);
				CheckDamage(hitpoints, normal.instance.hitpoints, 80.0, "The 3x3 aura must deal 80 damage to a normal zombie.");
				CheckDamage(hitpoints2, machine.instance.hitpoints, 160.0, "The 3x3 aura must double damage against a machine zombie.");
				CheckDamage(hitpoints3, metalArmor.instance.hitpoints, 160.0, "The 3x3 aura must double damage against a metallic-armor zombie.");
				CheckDamage(hitpoints4, bossMachine.instance.hitpoints, 160.0, "The 3x3 aura must still double damage against an electromagnetic boss.");
				Check(!normal.buff.BuffHas("EMSpeedDown") && machine.buff.BuffHas("EMSpeedDown") && metalArmor.buff.BuffHas("EMSpeedDown") && !bossMachine.buff.BuffHas("EMSpeedDown"), "Only non-boss electromagnetic targets in the aura may receive the slow.");
				Check(machine.BuffGet("EMSpeedDown")?.FrameMeshColorMultiplier == Colors.White, "The applied electromagnetic slow must leave the zombie untinted.");
				machine.timeScale = 1.0;
				machine.BuffGet("EMSpeedDown")?.Step(0.016);
				Check(Mathf.IsEqualApprox((float)machine.timeScale, 0.5f), "The electromagnetic slow must halve the target's effective time scale.");
				machine.buff.AddBuff(new TowerDefenseCharacterBuffIceSpeedDown
				{
					time = 1.0
				});
				machine.BatchUpdate(0.016);
				Check(machine.buff.BuffHas("IceSpeedDown") && !machine.buff.BuffHas("EMSpeedDown") && !machine.emSpeedDown, "Ice slow must replace electromagnetic slow instead of stacking with it.");
				machine.buff.DeleteBuff("IceSpeedDown");
				machine.instance.unUseBuffFlags |= 1;
				machine.buff.AddBuff(new TowerDefenseCharacterBuffEMSpeedDown
				{
					time = 1.0
				});
				Check(!machine.buff.BuffHas("EMSpeedDown") && !machine.emSpeedDown, "Targets immune to the shared ice-slow category must reject electromagnetic slow.");
				control.isGameRunning = false;
				control.characterNode.AddChild(partner, forceReadableName: false, InternalMode.Disabled);
				AddToCell(partner, _targetGrid);
				await WaitFrames(3);
				control.isGameRunning = true;
				double hitpoints5 = normal.instance.hitpoints;
				double hitpoints6 = machine.instance.hitpoints;
				double hitpoints7 = metalArmor.instance.hitpoints;
				double hitpoints8 = bossMachine.instance.hitpoints;
				source.BatchUpdate(0.016);
				CheckDamage(hitpoints5, normal.instance.hitpoints, 200.0, "A newly formed pair must immediately deal 200 current damage.");
				CheckDamage(hitpoints6, machine.instance.hitpoints, 400.0, "A newly formed pair must immediately double current damage against machines.");
				CheckDamage(hitpoints7, metalArmor.instance.hitpoints, 400.0, "A newly formed pair must double current damage against metallic armor.");
				CheckDamage(hitpoints8, bossMachine.instance.hitpoints, 400.0, "A newly formed pair must double current damage against an electromagnetic boss.");
				Check(!bossMachine.buff.BuffHas("EMP"), "Boss zombies must remain immune to the current's EMP control.");
				Check(source.sprite.clip == "anim_shooting" && partner.sprite.clip == "anim_shooting", "Both pair endpoints must play anim_shooting on the pulse frame.");
				Check(CountCurrentEffects(control.characterNode) == 1, "One horizontal pair must create exactly one current visual.");
				double hitpoints9 = normal.instance.hitpoints;
				double hitpoints10 = machine.instance.hitpoints;
				double hitpoints11 = metalArmor.instance.hitpoints;
				double hitpoints12 = bossMachine.instance.hitpoints;
				source.BatchUpdate(0.5);
				CheckDamage(hitpoints9, normal.instance.hitpoints, 0.0, "A zombie must not be hit twice by the same current pulse.");
				CheckDamage(hitpoints10, machine.instance.hitpoints, 0.0, "An enhanced zombie must not be hit twice by the same current pulse.");
				CheckDamage(hitpoints11, metalArmor.instance.hitpoints, 0.0, "A metallic-armor zombie must not be hit twice by the same current pulse.");
				CheckDamage(hitpoints12, bossMachine.instance.hitpoints, 0.0, "A boss must not be hit twice by the same current pulse.");
				Dictionary save = source.ExportVariantSave();
				Godot.Collections.Array array = save.GetValueOrDefault("pairTimers", new Godot.Collections.Array()).AsGodotArray();
				Dictionary dictionary = ((array.Count == 1) ? array[0].AsGodotDictionary() : new Dictionary());
				Check(array.Count == 1 && dictionary.GetValueOrDefault("active", 0.0).AsDouble() > 0.0 && dictionary.GetValueOrDefault("hitTargets", new Godot.Collections.Array()).AsGodotArray().Count == 4, "The owning endpoint must save its active pulse and stable hit-target identities.");
				Dictionary dictionary2 = source.ExportNetworkSpecialState();
				Godot.Collections.Array array2 = dictionary2.GetValueOrDefault("pairs", new Godot.Collections.Array()).AsGodotArray();
				Check(array2.Count == 1 && array2[0].AsGodotDictionary().GetValueOrDefault("active", 0.0).AsDouble() > 0.0, "The active pair must be exported for read-only clients and late joiners.");
				Dictionary networkJsonRoundTrip = Json.ParseString(Json.Stringify(dictionary2)).AsGodotDictionary();
				Dictionary dictionary3 = networkJsonRoundTrip["pairs"].AsGodotArray()[0].AsGodotDictionary();
				Check(dictionary3.GetValueOrDefault("vertical", true).VariantType == Variant.Type.Bool && !dictionary3.GetValueOrDefault("vertical", true).AsBool(), "Horizontal pair direction must survive the real JSON network round-trip.");
				source.BatchUpdate(0.08);
				Dictionary b = source.ExportNetworkSpecialState();
				bool condition = NetworkVariantComparer.DictionaryApproxEquals(dictionary2, b);
				Check(condition, "A running pulse must not produce continuous 0.08-second special-state packets.");
				bool previousMultiplayerMode = Global.Instance.isMultiplayerMode;
				bool previousHost = MultiPlayerManager.Instance.isHost;
				try
				{
					Global.Instance.isMultiplayerMode = true;
					MultiPlayerManager.Instance.isHost = false;
					clientSource = Instantiate<TowerDefensePlantEMPlantern>("res://Asset/Anime/Character/Plant/Chapter9/EMPlantern/Scene/TowerDefensePlantEMPlantern.tscn");
					PrepareCharacter(clientSource, _sourceGrid, manager);
					control.characterNode.AddChild(clientSource, forceReadableName: false, InternalMode.Disabled);
					await WaitFrames(2);
					int effectsBeforeClientImport = CountCurrentEffects(control.characterNode);
					clientSource.ImportNetworkSpecialState(networkJsonRoundTrip);
					clientSource.ImportNetworkSpecialState(networkJsonRoundTrip);
					int num = CountCurrentEffects(control.characterNode);
					int value = networkJsonRoundTrip["pairs"].AsGodotArray()[0].AsGodotDictionary().GetValueOrDefault("serial", 0).AsInt32();
					Check(clientSource.sprite.clip == "anim_shooting" && num == effectsBeforeClientImport + 1, "Read-only clients must create one idempotent current visual and play the pulse animation. " + $"clip={clientSource.sprite.clip} before={effectsBeforeClientImport} " + $"after={num} serial={value}");
					clientSource.ImportNetworkSpecialState(new Dictionary());
					await WaitFrames(2);
					Check(CountCurrentEffects(control.characterNode) == effectsBeforeClientImport, "Read-only clients must remove current visuals when the pair disappears from sync state.");
				}
				finally
				{
					if (GodotObject.IsInstanceValid(clientSource))
					{
						clientSource.Free();
					}
					clientSource = null;
					MultiPlayerManager.Instance.isHost = previousHost;
					Global.Instance.isMultiplayerMode = previousMultiplayerMode;
				}
				control.isGameRunning = false;
				RemoveFromCell(source);
				source.Free();
				source = Instantiate<TowerDefensePlantEMPlantern>("res://Asset/Anime/Character/Plant/Chapter9/EMPlantern/Scene/TowerDefensePlantEMPlantern.tscn");
				PrepareCharacter(source, _sourceGrid, manager);
				control.characterNode.AddChild(source, forceReadableName: false, InternalMode.Disabled);
				AddToCell(source, _sourceGrid);
				source.ImportVariantSave(save);
				Godot.Collections.Array array3 = source.ExportVariantSave().GetValueOrDefault("pairTimers", new Godot.Collections.Array()).AsGodotArray();
				Check(array3.Count == 1 && array3[0].AsGodotDictionary().GetValueOrDefault("active", 0.0).AsDouble() > 0.0 && array3[0].AsGodotDictionary().GetValueOrDefault("hitTargets", new Godot.Collections.Array()).AsGodotArray()
					.Count == 4, "Import followed by an immediate paused resave must retain the pending active pulse.");
				await WaitFrames(3);
				control.isGameRunning = true;
				double[] array4 = new double[4]
				{
					normal.instance.hitpoints,
					machine.instance.hitpoints,
					metalArmor.instance.hitpoints,
					bossMachine.instance.hitpoints
				};
				source.BatchUpdate(0.016);
				Check(Mathf.IsEqualApprox((float)array4[0], (float)normal.instance.hitpoints) && Mathf.IsEqualApprox((float)array4[1], (float)machine.instance.hitpoints) && Mathf.IsEqualApprox((float)array4[2], (float)metalArmor.instance.hitpoints) && Mathf.IsEqualApprox((float)array4[3], (float)bossMachine.instance.hitpoints), "Restoring an active pulse must not hit targets that were already recorded before saving.");
				Godot.Collections.Array array5 = source.ExportVariantSave().GetValueOrDefault("pairTimers", new Godot.Collections.Array()).AsGodotArray();
				Check(array5.Count == 1 && array5[0].AsGodotDictionary().GetValueOrDefault("active", 0.0).AsDouble() > 0.0 && array5[0].AsGodotDictionary().GetValueOrDefault("hitTargets", new Godot.Collections.Array()).AsGodotArray()
					.Count == 4, "An active current must round-trip its remaining window and stable hit-target set.");
				await VerifyHypnotizedCamps(control, manager);
				goto end_IL_00e8;
				end_IL_010b:;
			}
			catch (Exception value2)
			{
				_failures++;
				GD.PushError($"[PlantEMPlanternGameplayRuntimeTest] Unexpected exception: {value2}");
				goto end_IL_00e8;
			}
			return;
			end_IL_00e8:;
		}
		finally
		{
			control?.Set("isGameRunning", false);
			RemoveFromCell(source);
			RemoveFromCell(partner);
			RemoveFromCell(clientSource);
			RemoveFromCell(normal);
			RemoveFromCell(machine);
			RemoveFromCell(metalArmor);
			RemoveFromCell(bossMachine);
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(6);
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag = _failures == 0 && _checks == 55;
		GD.Print($"PLANT_EM_PLANTERN_GAMEPLAY_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyHypnotizedCamps(PlantEMPlanternGameplayRuntimeControlStub control, TowerDefenseManager manager)
	{
		control.isGameRunning = false;
		TowerDefensePlantEMPlantern normalSource = Instantiate<TowerDefensePlantEMPlantern>("res://Asset/Anime/Character/Plant/Chapter9/EMPlantern/Scene/TowerDefensePlantEMPlantern.tscn");
		TowerDefensePlantEMPlantern normalPartner = Instantiate<TowerDefensePlantEMPlantern>("res://Asset/Anime/Character/Plant/Chapter9/EMPlantern/Scene/TowerDefensePlantEMPlantern.tscn");
		TowerDefensePlantEMPlantern hypnoSource = Instantiate<TowerDefensePlantEMPlantern>("res://Asset/Anime/Character/Plant/Chapter9/EMPlantern/Scene/TowerDefensePlantEMPlantern.tscn");
		TowerDefensePlantEMPlantern hypnoPartner = Instantiate<TowerDefensePlantEMPlantern>("res://Asset/Anime/Character/Plant/Chapter9/EMPlantern/Scene/TowerDefensePlantEMPlantern.tscn");
		TowerDefenseZombie normalZombie = Instantiate<TowerDefenseZombie>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
		TowerDefenseZombie hypnoZombie = Instantiate<TowerDefenseZombie>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
		TowerDefenseCharacter[] characters = new TowerDefenseCharacter[6] { normalSource, normalPartner, hypnoSource, hypnoPartner, normalZombie, hypnoZombie };
		Vector2I[] array = new Vector2I[6]
		{
			new Vector2I(1, 1),
			new Vector2I(3, 1),
			new Vector2I(2, 1),
			new Vector2I(4, 1),
			new Vector2I(2, 1),
			new Vector2I(2, 1)
		};
		try
		{
			for (int i = 0; i < characters.Length; i++)
			{
				PrepareCharacter(characters[i], array[i], manager);
				control.characterNode.AddChild(characters[i], forceReadableName: false, InternalMode.Disabled);
				if (characters[i] is TowerDefensePlant)
				{
					AddToCell(characters[i], array[i]);
				}
			}
			await WaitFrames(3);
			TowerDefenseCharacter[] array2 = characters;
			foreach (TowerDefenseCharacter towerDefenseCharacter in array2)
			{
				towerDefenseCharacter.instance.hitpoints = (towerDefenseCharacter.instance.hitpointsBase = 10000.0);
			}
			hypnoSource.Hypnoses();
			hypnoZombie.Hypnoses();
			Check(hypnoSource.instance.hypnoses && hypnoSource.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE && hypnoZombie.instance.hypnoses && hypnoZombie.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT, "Real hypnosis must switch the plant and zombie to their opposing camps.");
			control.isGameRunning = true;
			normalSource.BatchUpdate(3.0);
			Check(HasOnlyPair(normalSource, normalPartner.gridPos), "A single hypnotized EM Plantern between normal endpoints must not disable or intercept their pair.");
			CheckDamage(10000.0, hypnoSource.instance.hitpoints, 280.0, "Normal aura and current must damage a hypnotized plant in their area.");
			CheckDamage(10000.0, normalZombie.instance.hitpoints, 280.0, "Normal aura and current must keep damaging zombies while a hypnotized plant is present.");
			CheckDamage(10000.0, hypnoZombie.instance.hitpoints, 0.0, "Normal aura and current must spare an allied hypnotized zombie.");
			CheckDamage(10000.0, normalPartner.instance.hitpoints, 0.0, "Normal current must spare its allied plant endpoint.");
			hypnoSource.BatchUpdate(3.0);
			Check(hypnoSource.ExportNetworkSpecialState()["pairs"].AsGodotArray().Count == 0, "An isolated hypnotized EM Plantern must not link to normal plants.");
			CheckDamage(10000.0, normalSource.instance.hitpoints, 80.0, "A hypnotized aura must damage an opposing plant.");
			CheckDamage(10000.0, normalPartner.instance.hitpoints, 80.0, "A hypnotized aura must damage opposing plants on both sides.");
			CheckDamage(10000.0, hypnoZombie.instance.hitpoints, 80.0, "A hypnotized aura must damage an opposing hypnotized zombie.");
			CheckDamage(9720.0, normalZombie.instance.hitpoints, 0.0, "A hypnotized aura must spare an allied normal zombie.");
			hypnoPartner.Hypnoses();
			double hitpoints = normalPartner.instance.hitpoints;
			double hitpoints2 = hypnoZombie.instance.hitpoints;
			hypnoSource.BatchUpdate(0.016);
			Check(HasOnlyPair(hypnoSource, hypnoPartner.gridPos) && HasOnlyPair(normalSource, normalPartner.gridPos), "Normal and hypnotized pairs must coexist and skip intervening opposing endpoints.");
			CheckDamage(hitpoints, normalPartner.instance.hitpoints, 200.0, "Hypnotized current must damage an opposing plant between its endpoints.");
			CheckDamage(hitpoints2, hypnoZombie.instance.hitpoints, 200.0, "Hypnotized current must damage an opposing hypnotized zombie.");
			CheckDamage(10000.0, hypnoPartner.instance.hitpoints, 0.0, "Hypnotized current must spare its allied plant endpoint.");
			CheckDamage(9720.0, normalZombie.instance.hitpoints, 0.0, "Hypnotized current must spare allied normal zombies.");
			hitpoints = normalPartner.instance.hitpoints;
			hypnoSource.BatchUpdate(0.016);
			CheckDamage(hitpoints, normalPartner.instance.hitpoints, 0.0, "An opposing plant must be hit only once per current pulse.");
			hypnoSource.Hypnoses();
			normalSource.BatchUpdate(0.016);
			hypnoSource.BatchUpdate(0.016);
			Check(!hypnoSource.instance.hypnoses && HasOnlyPair(normalSource, hypnoSource.gridPos) && HasOnlyPair(hypnoSource, normalPartner.gridPos), "Purifying an endpoint must remove its hostile link and reconnect the normal network.");
			hypnoSource.Hypnoses();
			normalSource.BatchUpdate(0.016);
			hypnoSource.BatchUpdate(0.016);
			Check(HasOnlyPair(normalSource, normalPartner.gridPos) && HasOnlyPair(hypnoSource, hypnoPartner.gridPos), "Reapplying hypnosis must restore two independent camp networks.");
			control.isGameRunning = false;
			await WaitFrames(2);
			bool previousMultiplayerMode = Global.Instance.isMultiplayerMode;
			bool previousHost = MultiPlayerManager.Instance.isHost;
			try
			{
				Global.Instance.isMultiplayerMode = true;
				MultiPlayerManager.Instance.isHost = false;
				control.isGameRunning = true;
				int effectsBeforeCampChange = CountCurrentEffects(control.characterNode);
				double plantBeforeClient = normalPartner.instance.hitpoints;
				hypnoPartner.buff.SyncDeserialize(new Dictionary { ["buffs"] = new Godot.Collections.Array() });
				hypnoSource.BatchUpdate(0.016);
				control.isGameRunning = false;
				await WaitFrames(2);
				Check(hypnoPartner.camp != hypnoSource.camp && CountCurrentEffects(control.characterNode) == effectsBeforeCampChange - 1, "A client must discard the old current visual when its partner changes camp before pair sync.");
				CheckDamage(plantBeforeClient, normalPartner.instance.hitpoints, 0.0, "Client camp and visual updates must not apply gameplay damage.");
			}
			finally
			{
				MultiPlayerManager.Instance.isHost = previousHost;
				Global.Instance.isMultiplayerMode = previousMultiplayerMode;
			}
		}
		finally
		{
			control.isGameRunning = false;
			TowerDefenseCharacter[] array2 = characters;
			foreach (TowerDefenseCharacter towerDefenseCharacter2 in array2)
			{
				RemoveFromCell(towerDefenseCharacter2);
				if (GodotObject.IsInstanceValid(towerDefenseCharacter2))
				{
					towerDefenseCharacter2.Free();
				}
			}
		}
	}

	private static bool HasOnlyPair(TowerDefensePlantEMPlantern source, Vector2I target)
	{
		Godot.Collections.Array array = source.ExportNetworkSpecialState()["pairs"].AsGodotArray();
		if (array.Count == 1 && array[0].AsGodotDictionary()["x"].AsInt32() == target.X)
		{
			return array[0].AsGodotDictionary()["y"].AsInt32() == target.Y;
		}
		return false;
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

	private static void PrepareCharacter(TowerDefenseCharacter character, Vector2I grid, TowerDefenseManager manager)
	{
		character.inGame = true;
		character.editorPreviewMode = false;
		character.gridPos = grid;
		character.cell = ((character is TowerDefenseZombie) ? null : TowerDefenseManager.GetMapCell(grid));
		character.Position = manager.GetMapCellPosCenter(grid);
	}

	private static void AddToCell(TowerDefenseCharacter character, Vector2I grid)
	{
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(grid);
		if (GodotObject.IsInstanceValid(mapCell) && !mapCell.characterList.Contains(character))
		{
			mapCell.characterList.Add(character);
		}
	}

	private static void RemoveFromCell(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character?.cell))
		{
			character.cell.characterList.Remove(character);
		}
	}

	private static int CountCurrentEffects(Node node)
	{
		int num = 0;
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			if (child.SceneFilePath == "res://Asset/Anime/Character/Plant/Chapter9/EMPlantern/Effect/Current/EMPlanternCurrent.tscn" || child.Name == (StringName)"EMPlanternCurrent")
			{
				num++;
			}
			num += CountCurrentEffects(child);
		}
		return num;
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig
			{
				gridNum = gridNum
			}
		});
		towerDefenseBattleFeatureMap.PlantGridInit();
		for (int i = 1; i <= gridNum.X; i++)
		{
			for (int j = 1; j <= gridNum.Y; j++)
			{
				towerDefenseBattleFeatureMap.GetPlantGridCell(new Vector2I(i, j)).Init(new TowerDefenseCellConfig());
			}
		}
		return towerDefenseBattleFeatureMap;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void CheckDamage(double before, double after, double expected, string message)
	{
		Check(Mathf.IsEqualApprox((float)(before - after), (float)expected), $"{message} actual={before - after:F3} expected={expected:F3}");
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[PlantEMPlanternGameplayRuntimeTest] " + message);
		}
	}
}
