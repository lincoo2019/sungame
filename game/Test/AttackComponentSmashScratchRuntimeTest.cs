using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class AttackComponentSmashScratchRuntimeTest : Node
{
	private static readonly Vector2I TestGrid = new Vector2I(3, 2);

	private const int ScratchPlantCount = 20;

	private readonly List<AttackSmashScratchPlantStub> _plants = new List<AttackSmashScratchPlantStub>();

	public override void _Ready()
	{
		bool flag = false;
		int checks = 0;
		int failures = 0;
		try
		{
			RunGate(ref checks, ref failures);
			flag = failures == 0;
		}
		catch (Exception ex)
		{
			failures++;
			GD.PrintErr("ATTACK_COMPONENT_SMASH_SCRATCH_EXCEPTION " + ex);
		}
		finally
		{
			ReleaseWorkload();
		}
		GD.Print($"ATTACK_COMPONENT_SMASH_SCRATCH_RESULT passed={flag} checks={checks} failures={failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void RunGate(ref int checks, ref int failures)
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		Check(ref checks, ref failures, GodotObject.IsInstanceValid(instance), "TowerDefenseManager autoload must be available.");
		if (!GodotObject.IsInstanceValid(instance))
		{
			return;
		}
		TowerDefenseControlNew currentControl = instance.currentControl;
		Vector2 gridBeginPos = instance.gridBeginPos;
		Vector2 gridSize = instance.gridSize;
		Vector2I gridNum = instance.gridNum;
		AttackSmashScratchControlStub attackSmashScratchControlStub = null;
		TowerDefenseMapControl towerDefenseMapControl = null;
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = null;
		TowerDefenseCharacter towerDefenseCharacter = null;
		ComponentManager componentManager = null;
		AttackComponent runtime = null;
		try
		{
			attackSmashScratchControlStub = new AttackSmashScratchControlStub
			{
				Name = "AttackSmashScratchControl",
				isGameRunning = true,
				isInit = true,
				levelConfig = new TowerDefenseLevelConfig()
			};
			AddChild(attackSmashScratchControlStub, forceReadableName: false, InternalMode.Disabled);
			attackSmashScratchControlStub.characterNode = new Node2D
			{
				Name = "CharacterNode"
			};
			attackSmashScratchControlStub.AddChild(attackSmashScratchControlStub.characterNode, forceReadableName: false, InternalMode.Disabled);
			instance.currentControl = attackSmashScratchControlStub;
			instance.gridBeginPos = Vector2.Zero;
			instance.gridSize = new Vector2(100f, 76f);
			instance.gridNum = new Vector2I(6, 4);
			towerDefenseMapControl = new TowerDefenseMapControl
			{
				Name = "MapControl"
			};
			towerDefenseBattleFeatureMap = CreateMapFeature(towerDefenseMapControl, instance.gridNum);
			towerDefenseBattleFeatureMap.control = attackSmashScratchControlStub;
			attackSmashScratchControlStub.featureDictionary[new StringName("Map")] = towerDefenseBattleFeatureMap;
			towerDefenseCharacter = CreateCharacter<AttackSmashScratchAttackerStub>(TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE, new Vector2(0f, 0f));
			towerDefenseCharacter.Name = "AttackScratchAttacker";
			towerDefenseCharacter.gridPos = TestGrid;
			towerDefenseCharacter.inGame = false;
			towerDefenseCharacter.SetHitBoxEnabled(enabled: true);
			AddChild(towerDefenseCharacter, forceReadableName: false, InternalMode.Disabled);
			instance.CharacterRegister(towerDefenseCharacter);
			AttackComponentDefinition attackComponentDefinition = new AttackComponentDefinition
			{
				ComponentTypeId = "AttackComponent",
				DefinitionId = "attack.smash.scratch.runtime",
				InstanceId = "attack.smash.scratch.runtime",
				WireIndex = 0,
				attackType = "Smash",
				checkGrid = false,
				checkLine = false,
				checkTall = false,
				checkEachShape = false,
				useZombieAttackCheck = false
			};
			attackComponentDefinition.checkShapeResources.Add(new AabbShape2DResource
			{
				Geometry = new RectangleShape2D
				{
					Size = new Vector2(500f, 500f)
				}
			});
			componentManager = new ComponentManager();
			runtime = new AttackComponent();
			runtime.Bind(componentManager, towerDefenseCharacter, attackComponentDefinition);
			runtime.Activate();
			towerDefenseCharacter.instance.collisionFlags = 1;
			towerDefenseCharacter.instance.maskFlags = towerDefenseCharacter.instance.collisionFlags;
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(TestGrid);
			Check(ref checks, ref failures, GodotObject.IsInstanceValid(mapCell), "Scratch map must expose the occupied test cell.");
			if (!GodotObject.IsInstanceValid(mapCell))
			{
				return;
			}
			for (int i = 0; i < 20; i++)
			{
				AttackSmashScratchPlantStub attackSmashScratchPlantStub = CreateCharacter<AttackSmashScratchPlantStub>(TowerDefenseEnum.CHARACTER_CAMP.PLANT, new Vector2(0f, 0f));
				attackSmashScratchPlantStub.Name = "AttackScratchPlant" + i;
				attackSmashScratchPlantStub.gridPos = TestGrid;
				attackSmashScratchPlantStub.cell = mapCell;
				attackSmashScratchPlantStub.SetHitBoxEnabled(enabled: true);
				mapCell.characterList.Add(attackSmashScratchPlantStub);
				mapCell.characterSlotDictionary[attackSmashScratchPlantStub] = null;
				instance.CharacterRegister(attackSmashScratchPlantStub);
				_plants.Add(attackSmashScratchPlantStub);
			}
			TowerDefenseCharacter target = runtime.GetTarget();
			Check(ref checks, ref failures, target is TowerDefensePlant, $"Scratch registry must resolve a plant target before SmashAttackCell; target={target?.Name ?? ((StringName)"<null>")}.");
			bool nestedInvoked = false;
			_plants[0].OnAttackDeal = delegate
			{
				if (!nestedInvoked)
				{
					nestedInvoked = true;
					runtime.SmashAttackCell(1.0);
				}
			};
			nestedInvoked = true;
			runtime.SmashAttackCell(1.0);
			foreach (AttackSmashScratchPlantStub plant in _plants)
			{
				plant.SmashHurtCalls = 0;
				plant.AttackDealCalls = 0;
			}
			nestedInvoked = true;
			for (int num = 0; num < 8; num++)
			{
				runtime.SmashAttackCell(1.0);
			}
			foreach (AttackSmashScratchPlantStub plant2 in _plants)
			{
				plant2.SmashHurtCalls = 0;
				plant2.AttackDealCalls = 0;
			}
			GC.Collect();
			long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
			for (int num2 = 0; num2 < 32; num2++)
			{
				runtime.GetTarget();
			}
			long value = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
			nestedInvoked = true;
			GC.Collect();
			long allocatedBytesForCurrentThread2 = GC.GetAllocatedBytesForCurrentThread();
			for (int num3 = 0; num3 < 32; num3++)
			{
				runtime.SmashAttackCell(1.0);
			}
			long value2 = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread2;
			nestedInvoked = false;
			GC.Collect();
			long allocatedBytesForCurrentThread3 = GC.GetAllocatedBytesForCurrentThread();
			runtime.SmashAttackCell(1.0);
			long value3 = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread3;
			GD.Print($"ATTACK_COMPONENT_SMASH_SCRATCH_ALLOC_SEGMENTS getTargetBytes={value} singleSmashBytes={value2} nestedSmashBytes={value3}");
			_plants[2].ThrowOnSmash = true;
			bool condition = false;
			try
			{
				runtime.SmashAttackCell(1.0);
			}
			catch (InvalidOperationException)
			{
				condition = true;
			}
			_plants[2].ThrowOnSmash = false;
			Check(ref checks, ref failures, condition, "SmashAttackCell must surface the intentional target exception.");
			foreach (AttackSmashScratchPlantStub plant3 in _plants)
			{
				plant3.SmashHurtCalls = 0;
				plant3.AttackDealCalls = 0;
			}
			nestedInvoked = false;
			long allocatedBytesForCurrentThread4 = GC.GetAllocatedBytesForCurrentThread();
			int num4 = GC.CollectionCount(0);
			for (int num5 = 0; num5 < 32; num5++)
			{
				nestedInvoked = num5 != 0;
				runtime.SmashAttackCell(1.0);
			}
			long num6 = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread4;
			int num7 = GC.CollectionCount(0) - num4;
			int expectedCallsPerPlant = 66;
			int value4 = _plants.Count * expectedCallsPerPlant;
			GD.Print($"ATTACK_COMPONENT_SMASH_SCRATCH_METRICS allocatedBytes={num6} gen0={num7} expectedTotalCalls={value4} expectedPerPlant={expectedCallsPerPlant} smashCalls={string.Join(',', _plants.ConvertAll((AttackSmashScratchPlantStub plant) => plant.SmashHurtCalls))} attackCalls={string.Join(',', _plants.ConvertAll((AttackSmashScratchPlantStub plant) => plant.AttackDealCalls))}");
			Check(ref checks, ref failures, num6 == 0, $"Warm SmashAttackCell calls must allocate zero bytes; allocated={num6}.");
			Check(ref checks, ref failures, num7 == 0, $"Warm SmashAttackCell calls must not trigger Gen0 GC; gen0={num7}.");
			Check(ref checks, ref failures, _plants.TrueForAll((AttackSmashScratchPlantStub plant) => plant.SmashHurtCalls == expectedCallsPerPlant), "Nested and expanded scratch scans must preserve each target's SmashHurt count.");
			Check(ref checks, ref failures, _plants.TrueForAll((AttackSmashScratchPlantStub plant) => plant.AttackDealCalls == expectedCallsPerPlant), "Nested and expanded scratch scans must preserve each target's AttackDeal count.");
		}
		finally
		{
			runtime?.Release();
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				instance.CharacterUnregister(towerDefenseCharacter);
				towerDefenseCharacter.Free();
			}
			foreach (AttackSmashScratchPlantStub plant4 in _plants)
			{
				if (GodotObject.IsInstanceValid(plant4))
				{
					instance.CharacterUnregister(plant4);
					plant4.Free();
				}
			}
			_plants.Clear();
			towerDefenseBattleFeatureMap?.Destroy();
			if (GodotObject.IsInstanceValid(towerDefenseMapControl))
			{
				towerDefenseMapControl.Free();
			}
			if (GodotObject.IsInstanceValid(attackSmashScratchControlStub))
			{
				attackSmashScratchControlStub.QueueFree();
			}
			instance.currentControl = currentControl;
			instance.gridBeginPos = gridBeginPos;
			instance.gridSize = gridSize;
			instance.gridNum = gridNum;
		}
	}

	private static T CreateCharacter<T>(TowerDefenseEnum.CHARACTER_CAMP camp, Vector2 position) where T : TowerDefenseCharacter, new()
	{
		T val = new T
		{
			Position = position,
			camp = camp
		};
		val.instance = new TowerDefenseCharacterInstance
		{
			character = val,
			hitpoints = 1000.0,
			hitpointsSave = 1000.0,
			collisionFlags = 1,
			maskFlags = 1,
			canBeCollection = true
		};
		val.HitBoxDefinition = new CharacterHitBoxDefinition
		{
			Size = new Vector2(40f, 40f)
		};
		val.targetRegistrationComponent = new TargetRegistrationComponent();
		return val;
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
		towerDefenseBattleFeatureMap.PlantGridInit();
		return towerDefenseBattleFeatureMap;
	}

	private static void Check(ref int checks, ref int failures, bool condition, string message)
	{
		checks++;
		if (!condition)
		{
			failures++;
			GD.PushError("[AttackComponentSmashScratchRuntimeTest] " + message);
		}
	}

	private void ReleaseWorkload()
	{
		foreach (AttackSmashScratchPlantStub plant in _plants)
		{
			if (GodotObject.IsInstanceValid(plant))
			{
				plant.Free();
			}
		}
		_plants.Clear();
	}
}
