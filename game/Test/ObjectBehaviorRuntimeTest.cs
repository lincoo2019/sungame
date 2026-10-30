using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class ObjectBehaviorRuntimeTest : Node
{
	private int _checks;

	private int _failures;

	private static readonly StringName DiscountBehaviorId = new StringName("test.card.discount-lock");

	private static readonly StringName CharacterBehaviorId = new StringName("test.character.effect-create");

	public override void _Ready()
	{
		try
		{
			TestRegistryAndLegacyCardCostFusion();
			TestCharacterRegistryComponentRouting();
			TestCardActionBehaviorMigration();
			TestLegacyLevelReaders();
			TestProjectileKernel();
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[ObjectBehaviorRuntimeTest] Unexpected exception: {value}");
		}
		finally
		{
			TowerDefenseBehaviorRegistry.UnregisterBehavior(DiscountBehaviorId);
			TowerDefenseBehaviorRegistry.UnregisterBehavior(CharacterBehaviorId);
			bool flag = _failures == 0;
			GD.Print($"OBJECT_BEHAVIOR_RESULT passed={flag} checks={_checks} failures={_failures}");
			GetTree().Quit((!flag) ? 1 : 0);
		}
	}

	private void TestCharacterRegistryComponentRouting()
	{
		EffectCreateComponentDefinition effectCreateComponentDefinition = new EffectCreateComponentDefinition
		{
			ComponentTypeId = "EffectCreateComponent",
			DefinitionId = CharacterBehaviorId.ToString(),
			InstanceId = "test.character.effect-create",
			WireIndex = 0
		};
		Check(TowerDefenseBehaviorRegistry.RegisterBehavior(effectCreateComponentDefinition), "Registry must accept a CharacterComponentDefinition by its DefinitionId.");
		EffectCreateComponentDefinition effectCreateComponentDefinition2 = new EffectCreateComponentDefinition
		{
			ComponentTypeId = "EffectCreateComponent",
			DefinitionId = "test.character.inline",
			InstanceId = "test.character.inline",
			WireIndex = 1
		};
		CharacterComponentSet obj = new CharacterComponentSet
		{
			BehaviorIds = { CharacterBehaviorId },
			Components = { (CharacterComponentDefinition)effectCreateComponentDefinition2 }
		};
		IReadOnlyList<CharacterComponentDefinition> flattenedDefinitions = obj.GetFlattenedDefinitions();
		Check(flattenedDefinitions.Count == 2 && flattenedDefinitions[0] == effectCreateComponentDefinition && flattenedDefinitions[1] == effectCreateComponentDefinition2, "Character component sets must merge Registry ids and inline definitions in stable order.");
		Check(flattenedDefinitions[0].CreateRuntime() is EffectCreateComponent && flattenedDefinitions[1].CreateRuntime() is EffectCreateComponent, "Resolved character behaviors must create the existing ComponentManager runtime type.");
		EffectCreateComponentDefinition effectCreateComponentDefinition3 = new EffectCreateComponentDefinition
		{
			ComponentTypeId = "EffectCreateComponent",
			DefinitionId = CharacterBehaviorId.ToString(),
			InstanceId = "test.character.effect-create",
			WireIndex = 0
		};
		TowerDefenseBehaviorRegistry.RegisterBehavior(CharacterBehaviorId, effectCreateComponentDefinition3);
		flattenedDefinitions = obj.GetFlattenedDefinitions();
		Check(flattenedDefinitions[0] == effectCreateComponentDefinition3, "Character component-set caches must refresh after a Registry replacement.");
	}

	private void TestRegistryAndLegacyCardCostFusion()
	{
		TowerDefensePacketChangeCost towerDefensePacketChangeCost = new TowerDefensePacketChangeCost
		{
			DefinitionId = DiscountBehaviorId,
			method = "Decrease",
			lockCost = true
		};
		towerDefensePacketChangeCost.amontDictionary[0] = 20;
		Check(TowerDefenseBehaviorRegistry.RegisterBehavior(DiscountBehaviorId, towerDefensePacketChangeCost), "Registry must accept a typed behavior definition.");
		Check(TowerDefenseBehaviorRegistry.GetBehavior<CardBehaviorDefinition>(DiscountBehaviorId) == towerDefensePacketChangeCost, "Typed registry lookup must resolve the registered card behavior.");
		TowerDefensePacketConfig obj = new TowerDefensePacketConfig
		{
			type = TowerDefenseEnum.PACKET_TYPE.WHITE,
			canChangeCost = true,
			behaviorIds = { DiscountBehaviorId }
		};
		TowerDefensePacketChangeCost towerDefensePacketChangeCost2 = new TowerDefensePacketChangeCost
		{
			method = "Increase"
		};
		towerDefensePacketChangeCost2.amontDictionary[0] = 25;
		obj.changeCostList.Add(towerDefensePacketChangeCost2);
		TowerDefensePacketChangeCost towerDefensePacketChangeCost3 = new TowerDefensePacketChangeCost
		{
			method = "Decrease"
		};
		towerDefensePacketChangeCost3.amontDictionary[0] = 5;
		obj.changeCostList.Add(towerDefensePacketChangeCost3);
		int num = CardBehaviorDispatcher.ApplyCost(obj, 100, skipGlobalChangeCost: true);
		Check(num == 75, $"Registered discount and legacy price rules must share one ordered lock-aware pipeline; result={num}.");
		towerDefensePacketChangeCost.skip = true;
		num = CardBehaviorDispatcher.ApplyCost(obj, -10, skipGlobalChangeCost: true);
		Check(num == -35, $"Legacy skip must stop only its registry stage, then allow packet rules and preserve authored negative costs; result={num}.");
	}

	private void TestCardActionBehaviorMigration()
	{
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = new TowerDefenseInGamePacketShow();
		TowerDefensePacketConfig towerDefensePacketConfig = new TowerDefensePacketConfig();
		ObjectBehaviorCountingPacketEvent objectBehaviorCountingPacketEvent = new ObjectBehaviorCountingPacketEvent();
		ObjectBehaviorCountingPacketEvent objectBehaviorCountingPacketEvent2 = new ObjectBehaviorCountingPacketEvent();
		ObjectBehaviorCountingPacketEvent objectBehaviorCountingPacketEvent3 = new ObjectBehaviorCountingPacketEvent();
		towerDefensePacketConfig.pressedActions.Add(objectBehaviorCountingPacketEvent);
		towerDefensePacketConfig.useSucceededActions.Add(objectBehaviorCountingPacketEvent2);
		towerDefensePacketConfig._override = new TowerDefensePacketOverride();
		towerDefensePacketConfig._override.useSucceededActions.Add(objectBehaviorCountingPacketEvent3);
		CardBehaviorHost cardBehaviorHost = new CardBehaviorHost();
		cardBehaviorHost.Bind(towerDefenseInGamePacketShow, towerDefensePacketConfig);
		cardBehaviorHost.NotifyPressed();
		cardBehaviorHost.NotifyUseSucceeded(null);
		Check(objectBehaviorCountingPacketEvent.Executions == 1, "The pressed action must execute through CardBehaviorHost.");
		Check(objectBehaviorCountingPacketEvent2.Executions == 0 && objectBehaviorCountingPacketEvent3.Executions == 1, "CardBehaviorHost must preserve packet override action precedence.");
		cardBehaviorHost.NotifyUseSucceeded(null, includeActions: false);
		Check(objectBehaviorCountingPacketEvent3.Executions == 1, "Generic card use may notify new behaviors without replaying action behaviors.");
		cardBehaviorHost.Release();
		towerDefenseInGamePacketShow.Free();
	}

	private void TestLegacyLevelReaders()
	{
		ObjectBehaviorCountingPacketEvent objectBehaviorCountingPacketEvent = new ObjectBehaviorCountingPacketEvent();
		Godot.Collections.Array from = new Godot.Collections.Array { objectBehaviorCountingPacketEvent };
		TowerDefensePacketConfig towerDefensePacketConfig = new TowerDefensePacketConfig();
		bool flag = towerDefensePacketConfig._Set(new StringName("eventPlant"), Variant.From(in from));
		Check(flag && towerDefensePacketConfig.useSucceededActions.Count == 1 && towerDefensePacketConfig.useSucceededActions[0] == objectBehaviorCountingPacketEvent && objectBehaviorCountingPacketEvent.triggerFlags == 2, "Removed eventPlant data must migrate into useSucceededActions while loading.");
		Dictionary data = new Dictionary { ["EventPlant"] = new Godot.Collections.Array
		{
			new Dictionary
			{
				["EventName"] = "ChangeCost",
				["Value"] = new Dictionary
				{
					["Method"] = "ADD",
					["Value"] = 25.0
				}
			}
		} };
		TowerDefensePacketOverride towerDefensePacketOverride = new TowerDefensePacketOverride();
		towerDefensePacketOverride.Init(data);
		Check(towerDefensePacketOverride.useSucceededActions.Count == 1 && towerDefensePacketOverride.useSucceededActions[0] is CardActionBehaviorChangeCost cardActionBehaviorChangeCost && Math.Abs(cardActionBehaviorChangeCost.value - 25.0) < 0.001, "Legacy EventPlant JSON must load through the new action factory.");
		Dictionary dictionary = towerDefensePacketOverride.Export();
		Check(dictionary.ContainsKey("UseSucceededActions") && !dictionary.ContainsKey("EventPlant"), "New override saves must emit only the Registry action schema.");
		ProjectileBehaviorYMoveSin projectileBehaviorYMoveSin = new ProjectileBehaviorYMoveSin();
		Godot.Collections.Array from2 = new Godot.Collections.Array { projectileBehaviorYMoveSin };
		TowerDefenseProjectileConfig towerDefenseProjectileConfig = new TowerDefenseProjectileConfig();
		bool flag2 = towerDefenseProjectileConfig._Set(new StringName("methods"), Variant.From(in from2));
		Check(flag2 && towerDefenseProjectileConfig.behaviors.Count == 1 && towerDefenseProjectileConfig.behaviors[0] == projectileBehaviorYMoveSin, "Removed projectile methods data must migrate into behaviors while loading.");
	}

	private void TestProjectileKernel()
	{
		ProjectileBehaviorKernel projectileBehaviorKernel = new ProjectileBehaviorYMoveSin
		{
			Strength = 20.0,
			Speed = 2.0
		}.CreateBulletFieldKernel(4);
		BulletData bullet = new BulletData
		{
			savePos = new Vector2(10f, 100f),
			pos = new Vector2(10f, 100f)
		};
		projectileBehaviorKernel.OnSpawn(2, ref bullet);
		projectileBehaviorKernel.Process(2, ref bullet, Math.PI / 4.0);
		Check(Math.Abs(bullet.pos.Y - 120f) < 0.001f, $"Sine projectile behavior must run through its slot-indexed BulletField kernel; y={bullet.pos.Y}.");
		projectileBehaviorKernel.OnDespawn(2, ref bullet);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[ObjectBehaviorRuntimeTest] " + message);
		}
	}
}
