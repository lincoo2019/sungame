using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterSaveConfigCSharp : Resource
{
	[Export(PropertyHint.None, "")]
	public StringName nodeName;

	[Export(PropertyHint.None, "")]
	public string packetName;

	[Export(PropertyHint.None, "")]
	public Vector2 pos;

	[Export(PropertyHint.None, "")]
	public Vector2I gridPos;

	[Export(PropertyHint.None, "")]
	public double height;

	[Export(PropertyHint.None, "")]
	public string economyOwnerAccountId = "";

	[Export(PropertyHint.None, "")]
	public TowerDefenseNodeSaveConfigCSharp characterNodeSave;

	[Export(PropertyHint.None, "")]
	public Dictionary instanceSave = new Dictionary();

	[Export(PropertyHint.None, "")]
	public Array<Dictionary> buffSave = new Array<Dictionary>();

	[Export(PropertyHint.None, "")]
	public Array<string> currentArmor = new Array<string>();

	[Export(PropertyHint.None, "")]
	public Array<string> currentCustom = new Array<string>();

	[Export(PropertyHint.None, "")]
	public double timeScale = 1.0;

	[Export(PropertyHint.None, "")]
	public double timeScaleInit = 1.0;

	[Export(PropertyHint.None, "")]
	public double timeScaleSave = 1.0;

	[Export(PropertyHint.None, "")]
	public Dictionary stateMachineSave = new Dictionary();

	[Export(PropertyHint.None, "")]
	public Dictionary stateChartSave = new Dictionary();

	[Export(PropertyHint.None, "")]
	public Dictionary spriteSave = new Dictionary();

	[Export(PropertyHint.None, "")]
	public Array<Dictionary> componentSaveList = new Array<Dictionary>();

	[Export(PropertyHint.None, "")]
	public Dictionary characterFlags = new Dictionary();

	[Export(PropertyHint.None, "")]
	public Dictionary zombieExtraSave = new Dictionary();

	[Export(PropertyHint.None, "")]
	public double scaleX = 1.0;

	[Export(PropertyHint.None, "")]
	public double scaleY = 1.0;

	[Export(PropertyHint.None, "")]
	public double transformPointScaleX = 1.0;

	[Export(PropertyHint.None, "")]
	public double transformPointScaleY = 1.0;

	[Export(PropertyHint.None, "")]
	public Dictionary overrideSave = new Dictionary();

	[Export(PropertyHint.None, "")]
	public Dictionary variantSave = new Dictionary();

	[Export(PropertyHint.None, "")]
	public bool canChangeCostSave = true;

	[Export(PropertyHint.None, "")]
	public Array<Dictionary> changeCostListSave = new Array<Dictionary>();

	[Export(PropertyHint.None, "")]
	public double z;

	[Export(PropertyHint.None, "")]
	public double ySpeed;

	[Export(PropertyHint.None, "")]
	public bool isGround = true;

	[Export(PropertyHint.None, "")]
	public double cellPercentage = 0.5;

	[Export(PropertyHint.None, "")]
	public double cost;

	public TowerDefenseLevelSaveConfigCSharp owner;

	public void SaveCharacter(TowerDefenseCharacter character)
	{
		owner = null;
		nodeName = new StringName(character.Name.ToString().ValidateNodeName());
		packetName = character.config.name;
		pos = character.GetLogicalGlobalPosition();
		gridPos = character.gridPos;
		height = character.groundHeight;
		economyOwnerAccountId = (character.HasEconomyOwner ? character.EconomyOwnerAccountId.Value : "");
		instanceSave = character.instance.ExportSave();
		BuffComponent buff = character.buff;
		buffSave = ((buff != null && !buff.IsReleased) ? character.buff.ExportSave() : new Array<Dictionary>());
		currentArmor.Clear();
		foreach (TowerDefenseArmorInstance armor in character.instance.armorList)
		{
			if (!armor.isRemove)
			{
				currentArmor.Add(armor.slotConfig.armorName);
			}
		}
		currentCustom = character.currentCustom;
		timeScale = character.timeScale;
		timeScaleInit = character.timeScaleInit;
		timeScaleSave = character.timeScaleSave;
		scaleX = character.Scale.X;
		scaleY = character.Scale.Y;
		if (GodotObject.IsInstanceValid(character.transformPoint))
		{
			transformPointScaleX = character.transformPoint.Scale.X;
			transformPointScaleY = character.transformPoint.Scale.Y;
		}
		if (GodotObject.IsInstanceValid(character.packet) && GodotObject.IsInstanceValid(character.packet._override))
		{
			overrideSave = character.packet._override.Export();
		}
		else
		{
			overrideSave = new Dictionary();
		}
		if (GodotObject.IsInstanceValid(character.packet))
		{
			canChangeCostSave = character.packet.canChangeCost;
			changeCostListSave.Clear();
			foreach (TowerDefensePacketChangeCost changeCost in character.packet.changeCostList)
			{
				changeCostListSave.Add(changeCost.ExportSave());
			}
		}
		stateMachineSave = character.CaptureMainStateMachineSnapshotData();
		stateChartSave.Clear();
		if (GodotObject.IsInstanceValid(character.sprite))
		{
			spriteSave = character.sprite.ExportSpriteSave();
		}
		componentSaveList.Clear();
		if (GodotObject.IsInstanceValid(character.componentManager))
		{
			foreach (ComponentBase component in character.componentManager.componentList)
			{
				Dictionary dictionary = component.ExportComponentSave() ?? new Dictionary();
				if (!component.alive)
				{
					dictionary["_alive"] = false;
				}
				Dictionary dictionary2 = component.CaptureStateMachineSnapshotData();
				if (dictionary2.Count > 0)
				{
					dictionary["_stateMachine"] = dictionary2;
				}
				if (dictionary.Count > 0)
				{
					dictionary["_componentName"] = component.Name;
					if (character.componentManager.TryGetWireKey(component, out var wireKey))
					{
						dictionary["_componentKey"] = wireKey;
					}
					componentSaveList.Add(dictionary);
				}
			}
			foreach (CharacterComponentRuntime resourceComponent in character.componentManager.ResourceComponents)
			{
				if (resourceComponent == null || resourceComponent.IsReleased)
				{
					continue;
				}
				Dictionary dictionary3 = resourceComponent.ExportComponentSave()?.Duplicate(deep: true) ?? new Dictionary();
				dictionary3["_alive"] = resourceComponent.Alive;
				Dictionary dictionary4 = resourceComponent.CaptureStateMachineSnapshotData();
				if (dictionary4.Count > 0)
				{
					dictionary3["_stateMachine"] = dictionary4;
				}
				if (dictionary3.Count != 0)
				{
					CharacterComponentDefinition componentDefinition = resourceComponent.ComponentDefinition;
					if (character.componentManager.TryGetWireKey(resourceComponent, out var wireKey2))
					{
						dictionary3["_componentKey"] = wireKey2;
					}
					if (!string.IsNullOrEmpty(componentDefinition?.InstanceId))
					{
						dictionary3["_componentInstanceId"] = componentDefinition.InstanceId;
					}
					if (!string.IsNullOrEmpty(componentDefinition?.DefinitionId))
					{
						dictionary3["_componentDefinitionId"] = componentDefinition.DefinitionId;
					}
					if (componentDefinition != null)
					{
						dictionary3["_componentSchemaVersion"] = componentDefinition.SchemaVersion;
					}
					componentSaveList.Add(dictionary3);
				}
			}
		}
		characterFlags = new Dictionary
		{
			["componentAlive"] = character.componentAlive,
			["componentRunning"] = character.componentRunning,
			["invisible"] = character.invisible,
			["inWater"] = character.inWater,
			["iceSpeedDown"] = character.iceSpeedDown,
			["isRise"] = character.isRise,
			["isShovel"] = character.isShovel,
			["isSmash"] = character.isSmash,
			["isExplode"] = character.isExplode,
			["isChomp"] = character.isChomp,
			["isUnlimitedFire"] = character.isUnlimitedFire,
			["useIdleAnimeReset"] = character.useIdleAnimeReset,
			["nearDie"] = character.nearDie,
			["canMowerMove"] = character.canMowerMove,
			["die"] = character.die,
			["inGame"] = character.inGame,
			["characterFilter"] = character.characterFilter,
			["componentGameplayUntilBattlefieldEntry"] = character.HasComponentGameplayUntilBattlefieldEntry
		};
		ShaderEffectComponent shaderEffectComponent = character.shaderEffectComponent;
		if (shaderEffectComponent != null && !shaderEffectComponent.IsReleased)
		{
			characterFlags["effectFlags"] = character.shaderEffectComponent.GetEffectFlags();
		}
		z = character.z;
		ySpeed = character.ySpeed;
		isGround = character.isGround;
		cellPercentage = character.cellPercentage;
		cost = character.cost;
		if (character.HasMeta("trio_coral"))
		{
			characterFlags["trio_coral"] = true;
		}
		variantSave = character.ExportVariantSave();
		if (character is TowerDefenseZombie towerDefenseZombie)
		{
			zombieExtraSave = new Dictionary
			{
				["isPause"] = towerDefenseZombie.isPause,
				["isGarlic"] = towerDefenseZombie.isGarlic,
				["isChangeLine"] = towerDefenseZombie.isChangeLine,
				["inSwimPlay"] = towerDefenseZombie.inSwimPlay,
				["inGround"] = towerDefenseZombie.inGround,
				["startAttack"] = towerDefenseZombie.startAttack,
				["sizeUpNum"] = towerDefenseZombie.sizeUpNum,
				["hasGhost"] = towerDefenseZombie.hasGhost,
				["hasSpikeball"] = towerDefenseZombie.hasSpikeball,
				["spritePause"] = towerDefenseZombie.spritePause,
				["walkSpeedScale"] = towerDefenseZombie.walkSpeedScale
			};
		}
	}

	private double RestoreFinite(double value, double fallback)
	{
		if (double.IsFinite(value) && value <= 3.4028234663852886E+38 && value >= -3.4028234663852886E+38)
		{
			return value;
		}
		owner.RestoreReport.Record("CharacterState", "Invalid character number; using default.");
		return fallback;
	}

	public TowerDefenseCharacter InstantiateCharacterForRestore()
	{
		if (owner == null)
		{
			owner = new TowerDefenseLevelSaveConfigCSharp();
		}
		if (characterFlags == null)
		{
			characterFlags = new Dictionary();
		}
		if (zombieExtraSave == null)
		{
			zombieExtraSave = new Dictionary();
		}
		if (variantSave == null)
		{
			variantSave = new Dictionary();
		}
		if (stateMachineSave == null)
		{
			stateMachineSave = new Dictionary();
		}
		if (stateChartSave == null)
		{
			stateChartSave = new Dictionary();
		}
		if (spriteSave == null)
		{
			spriteSave = new Dictionary();
		}
		if (currentArmor == null)
		{
			currentArmor = new Array<string>();
		}
		if (currentCustom == null)
		{
			currentCustom = new Array<string>();
		}
		if (!pos.IsFinite())
		{
			pos = Vector2.Zero;
			owner.RestoreReport.Record("CharacterState", "Invalid character position.");
		}
		height = RestoreFinite(height, 0.0);
		z = RestoreFinite(z, 0.0);
		ySpeed = RestoreFinite(ySpeed, 0.0);
		scaleX = RestoreFinite(scaleX, 1.0);
		scaleY = RestoreFinite(scaleY, 1.0);
		transformPointScaleX = RestoreFinite(transformPointScaleX, 1.0);
		transformPointScaleY = RestoreFinite(transformPointScaleY, 1.0);
		timeScale = RestoreFinite(timeScale, 1.0);
		timeScaleInit = RestoreFinite(timeScaleInit, 1.0);
		timeScaleSave = RestoreFinite(timeScaleSave, 1.0);
		EconomyAccountId accountId = default(EconomyAccountId);
		if (!string.IsNullOrWhiteSpace(economyOwnerAccountId) && (!EconomyAccountId.TryParse(economyOwnerAccountId, out accountId) || !accountId.IsLocal))
		{
			owner.RestoreReport.Record("CharacterState", "Using local character ownership.");
			accountId = default(EconomyAccountId);
		}
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		TowerDefensePacketConfig packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly(packetName);
		if (!GodotObject.IsInstanceValid(packetConfigReadOnly))
		{
			return null;
		}
		TowerDefensePacketConfig towerDefensePacketConfig = packetConfigReadOnly.Duplicate(deep: true) as TowerDefensePacketConfig;
		if (!GodotObject.IsInstanceValid(towerDefensePacketConfig))
		{
			return null;
		}
		Dictionary dictionary = overrideSave;
		if (dictionary != null && dictionary.Count > 0)
		{
			TowerDefensePacketOverride towerDefensePacketOverride = new TowerDefensePacketOverride();
			towerDefensePacketOverride.Init(overrideSave);
			towerDefensePacketConfig._override = towerDefensePacketOverride;
		}
		towerDefensePacketConfig.canChangeCost = canChangeCostSave;
		towerDefensePacketConfig.changeCostList = new List<TowerDefensePacketChangeCost>();
		foreach (Dictionary item in changeCostListSave ?? new Array<Dictionary>())
		{
			towerDefensePacketConfig.changeCostList.Add(TowerDefensePacketChangeCost.ImportSave(item));
		}
		TowerDefenseCharacter towerDefenseCharacter = towerDefensePacketConfig.Create(pos, gridPos, height);
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			return null;
		}
		try
		{
			if (accountId.IsValid && !towerDefenseCharacter.TryAssignEconomyOwner(accountId))
			{
				towerDefenseCharacter.QueueFree();
				return null;
			}
			if (nodeName != null && !nodeName.IsEmpty)
			{
				towerDefenseCharacter.Name = nodeName;
			}
			towerDefenseCharacter.groundHeight = height;
			towerDefenseCharacter.currentArmor = currentArmor;
			towerDefenseCharacter.currentCustom = currentCustom;
			towerDefenseCharacter.packet = towerDefensePacketConfig;
			towerDefenseCharacter.PrepareForProgressRestore();
			characterNode.AddChild(towerDefenseCharacter, forceReadableName: false, Node.InternalMode.Disabled);
			return towerDefenseCharacter;
		}
		catch
		{
			TowerDefenseLevelSaveConfigCSharp.DiscardRestoredCharacter(towerDefenseCharacter);
			throw;
		}
	}

	public void RestoreCharacter(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character))
		{
			return;
		}
		ProgressRestoreReport restoreReport = owner?.RestoreReport ?? new ProgressRestoreReport();
		character.RestoreFromSave(this);
		character.Scale = new Vector2((float)scaleX, (float)scaleY);
		if (GodotObject.IsInstanceValid(character.transformPoint))
		{
			character.transformPoint.Scale = new Vector2((float)transformPointScaleX, (float)transformPointScaleY);
		}
		ShadowComponent shadowComponent = character.shadowComponent;
		if (shadowComponent != null && !shadowComponent.IsReleased)
		{
			character.shadowComponent.Init();
		}
		WaterInteractionComponent waterInteractionComponent = (GodotObject.IsInstanceValid(character.componentManager) ? character.componentManager.GetRuntime<WaterInteractionComponent>() : null);
		if (waterInteractionComponent != null && !waterInteractionComponent.IsReleased)
		{
			waterInteractionComponent.saveTransformPointScaleY = character.transformPoint.Scale.Y;
			waterInteractionComponent.saveSpriteGroupScaleY = character.spriteGroup.Scale.Y;
		}
		character.isGround = isGround;
		character.z = z;
		character.ySpeed = ySpeed;
		character.cellPercentage = cellPercentage;
		character.cost = cost;
		character.componentAlive = characterFlags.GetValueOrDefault("componentAlive", true).AsBool();
		character.componentRunning = characterFlags.GetValueOrDefault("componentRunning", false).AsBool();
		character.invisible = characterFlags.GetValueOrDefault("invisible", false).AsBool();
		character.iceSpeedDown = characterFlags.GetValueOrDefault("iceSpeedDown", false).AsBool();
		character.isRise = characterFlags.GetValueOrDefault("isRise", false).AsBool();
		character.isShovel = characterFlags.GetValueOrDefault("isShovel", false).AsBool();
		character.isSmash = characterFlags.GetValueOrDefault("isSmash", false).AsBool();
		character.isExplode = characterFlags.GetValueOrDefault("isExplode", false).AsBool();
		character.isChomp = characterFlags.GetValueOrDefault("isChomp", false).AsBool();
		character.isUnlimitedFire = characterFlags.GetValueOrDefault("isUnlimitedFire", false).AsBool();
		character.useIdleAnimeReset = characterFlags.GetValueOrDefault("useIdleAnimeReset", true).AsBool();
		character.nearDie = characterFlags.GetValueOrDefault("nearDie", false).AsBool();
		character.canMowerMove = characterFlags.GetValueOrDefault("canMowerMove", false).AsBool();
		character.die = characterFlags.GetValueOrDefault("die", false).AsBool();
		character.inGame = characterFlags.GetValueOrDefault("inGame", true).AsBool();
		character.characterFilter = characterFlags.GetValueOrDefault("characterFilter", false).AsBool();
		if (characterFlags.GetValueOrDefault("componentGameplayUntilBattlefieldEntry", false).AsBool())
		{
			character.EnableComponentGameplayUntilBattlefieldEntry();
		}
		foreach (Dictionary item in componentSaveList ?? new Array<Dictionary>())
		{
			try
			{
				string text = item.GetValueOrDefault("_componentName", "").AsString();
				string text2 = item.GetValueOrDefault("_componentKey", "").AsString();
				string text3 = item.GetValueOrDefault("_componentInstanceId", "").AsString();
				item.GetValueOrDefault("_componentDefinitionId", "").AsString();
				item.GetValueOrDefault("_componentSchemaVersion", 0).AsInt32();
				CharacterComponentRuntime runtime = null;
				if (text3 != "")
				{
					character.componentManager.TryGetRuntimeByInstanceId(text3, out runtime);
				}
				else if (text2 != "")
				{
					character.componentManager.TryGetRuntimeByWireKey(text2, out runtime);
				}
				if (runtime == null && text3 == "" && text != "")
				{
					character.componentManager.TryGetRuntimeByLegacyNodeName(text, out runtime);
				}
				if (runtime != null)
				{
					StateMachineSnapshot snapshot = null;
					bool flag = item.ContainsKey("_stateMachine") && ComponentBase.TryDecodeStateMachineSnapshot(item["_stateMachine"].AsGodotDictionary(), out snapshot) && runtime.CanRestoreStateMachineSnapshot(snapshot, progressCompatible: true);
					if (item.ContainsKey("_stateMachine") && !flag)
					{
						restoreReport.Record("Component", "Using initial component state.");
					}
					if (flag)
					{
						runtime.BeginAuthoritativeStateRestore();
					}
					try
					{
						runtime.ImportComponentSave(item, owner);
					}
					finally
					{
						if (flag)
						{
							runtime.EndAuthoritativeStateRestore();
						}
					}
					if (flag)
					{
						runtime.RestoreStateMachineSnapshot(snapshot, remote: false, suppressEntryEffects: true, progressCompatible: true);
					}
					if (item.ContainsKey("_alive"))
					{
						runtime.SetAlive(item["_alive"].AsBool());
					}
					continue;
				}
				if (text3 != "")
				{
					restoreReport.Record("Component", "Skipped missing resource component save '" + text3 + "'.");
					continue;
				}
				ComponentBase component = null;
				if (text2 != "")
				{
					character.componentManager.TryGetComponentByWireKey(text2, out component);
				}
				if (!GodotObject.IsInstanceValid(component) && text != "")
				{
					component = character.componentManager.GetComponentFromName(text);
				}
				if (GodotObject.IsInstanceValid(component))
				{
					StateMachineSnapshot snapshot2 = null;
					bool flag2 = item.ContainsKey("_stateMachine") && ComponentBase.TryDecodeStateMachineSnapshot(item["_stateMachine"].AsGodotDictionary(), out snapshot2) && component.CanRestoreStateMachineSnapshot(snapshot2, progressCompatible: true);
					if (item.ContainsKey("_stateMachine") && !flag2)
					{
						restoreReport.Record("Component", "Using initial component state.");
					}
					if (flag2)
					{
						component.BeginAuthoritativeStateRestore();
					}
					try
					{
						component.ImportComponentSave(item, owner);
					}
					finally
					{
						if (flag2)
						{
							component.EndAuthoritativeStateRestore();
						}
					}
					if (flag2)
					{
						component.RestoreStateMachineSnapshot(snapshot2, remote: false, suppressEntryEffects: true, progressCompatible: true);
					}
					if (item.ContainsKey("_alive"))
					{
						component.SetAlive(item["_alive"].AsBool());
					}
				}
				else
				{
					restoreReport.Record("Component", "Missing component: " + text + ".");
				}
			}
			catch (Exception ex)
			{
				restoreReport.Record("Component", ex.Message);
			}
		}
		character.inWater = characterFlags.GetValueOrDefault("inWater", false).AsBool();
		if (character is TowerDefenseZombie towerDefenseZombie && zombieExtraSave.Count > 0)
		{
			towerDefenseZombie.isGarlic = zombieExtraSave.GetValueOrDefault("isGarlic", false).AsBool();
			towerDefenseZombie.isChangeLine = zombieExtraSave.GetValueOrDefault("isChangeLine", false).AsBool();
			towerDefenseZombie.inSwimPlay = zombieExtraSave.GetValueOrDefault("inSwimPlay", false).AsBool();
			towerDefenseZombie.inGround = zombieExtraSave.GetValueOrDefault("inGround", false).AsBool();
			towerDefenseZombie.startAttack = zombieExtraSave.GetValueOrDefault("startAttack", false).AsBool();
			towerDefenseZombie.sizeUpNum = zombieExtraSave.GetValueOrDefault("sizeUpNum", 2).AsInt32();
			towerDefenseZombie.hasGhost = zombieExtraSave.GetValueOrDefault("hasGhost", false).AsBool();
			towerDefenseZombie.hasSpikeball = zombieExtraSave.GetValueOrDefault("hasSpikeball", false).AsBool();
			towerDefenseZombie.walkSpeedScale = zombieExtraSave.GetValueOrDefault("walkSpeedScale", 1.0).AsDouble();
		}
		if (variantSave.Count > 0 || character.ImportVariantSaveWhenEmpty())
		{
			restoreReport.Try("CharacterState", delegate
			{
				character.ImportVariantSave(variantSave);
			});
		}
		restoreReport.Try("CharacterState", delegate
		{
			if (stateMachineSave.Count > 0)
			{
				if (!character.RestoreMainStateMachineSnapshotData(stateMachineSave, remote: false, suppressEntryEffects: false, progressCompatible: true))
				{
					restoreReport.Record("CharacterState", "Using initial character state.");
				}
				character.timeScaleInit = timeScaleInit;
				character.timeScaleSave = timeScaleSave;
				if (character is TowerDefensePlant && GodotObject.IsInstanceValid(character.sprite))
				{
					character.sprite.timeScale = timeScale;
				}
			}
			else
			{
				StateChart nodeOrNull = character.GetNodeOrNull<StateChart>("StateChart");
				string stateName;
				if (nodeOrNull != null && GodotObject.IsInstanceValid(nodeOrNull.CurrentState) && stateChartSave.Count > 0)
				{
					SavedState savedState = new SavedState
					{
						childStates = stateChartSave.GetValueOrDefault("child_states", new Dictionary()).AsGodotDictionary(),
						pendingTransitionName = new NodePath(stateChartSave.GetValueOrDefault("pending_transition_name", "").AsString()),
						pendingTransitionRemainingDelay = (float)stateChartSave.GetValueOrDefault("pending_transition_remaining_delay", 0.0).AsDouble(),
						pendingTransitionInitialDelay = (float)stateChartSave.GetValueOrDefault("pending_transition_initial_delay", 0.0).AsDouble()
					};
					nodeOrNull.CurrentState.StateRestore(savedState);
					character.timeScaleInit = timeScaleInit;
					character.timeScaleSave = timeScaleSave;
					if (character is TowerDefensePlant && GodotObject.IsInstanceValid(character.sprite))
					{
						character.sprite.timeScale = timeScale;
					}
				}
				else if (stateChartSave.Count > 0 && TryGetLegacyActiveLeaf(stateChartSave.GetValueOrDefault("child_states", new Dictionary()).AsGodotDictionary(), out stateName))
				{
					character.RestoreLegacyMainState(stateName);
					character.timeScaleInit = timeScaleInit;
					character.timeScaleSave = timeScaleSave;
					if (character is TowerDefensePlant && GodotObject.IsInstanceValid(character.sprite))
					{
						character.sprite.timeScale = timeScale;
					}
				}
			}
		});
		if (GodotObject.IsInstanceValid(character.sprite) && spriteSave.Count > 0)
		{
			restoreReport.Try("CharacterState", delegate
			{
				character.sprite.ImportSpriteSave(spriteSave);
			});
		}
		if (character is TowerDefenseZombie towerDefenseZombie2 && zombieExtraSave.Count > 0)
		{
			towerDefenseZombie2.spritePause = zombieExtraSave.GetValueOrDefault("spritePause", false).AsBool();
			towerDefenseZombie2.isPause = false;
		}
		if (characterFlags.ContainsKey("effectFlags"))
		{
			ShaderEffectComponent shaderEffectComponent = character.shaderEffectComponent;
			if (shaderEffectComponent != null && !shaderEffectComponent.IsReleased)
			{
				character.shaderEffectComponent.SetEffectFlags(characterFlags.GetValueOrDefault("effectFlags", 0).AsInt32());
			}
		}
		character.FinalizeProgressRestore();
		if (character is TowerDefenseZombie zombie && characterFlags.GetValueOrDefault("trio_coral", false).AsBool())
		{
			TrioAmbushMember.Attach(zombie, coral: true, 0.0, null, -1, restored: true);
		}
	}

	public TowerDefenseCharacter LoadCharacter()
	{
		TowerDefenseCharacter towerDefenseCharacter = InstantiateCharacterForRestore();
		RestoreCharacter(towerDefenseCharacter);
		return towerDefenseCharacter;
	}

	private static bool TryGetLegacyActiveLeaf(Dictionary childStates, out string stateName)
	{
		stateName = string.Empty;
		if (childStates == null || childStates.Count == 0)
		{
			return false;
		}
		foreach (Variant key in childStates.Keys)
		{
			string text = key.AsString();
			if (!string.IsNullOrWhiteSpace(text))
			{
				stateName = text;
				Variant variant = childStates[key];
				Dictionary dictionary = null;
				if (variant.VariantType == Variant.Type.Object && variant.As<GodotObject>() is SavedState savedState)
				{
					dictionary = savedState.childStates;
				}
				else if (variant.VariantType == Variant.Type.Dictionary)
				{
					Dictionary dictionary2 = variant.AsGodotDictionary();
					dictionary = (dictionary2.ContainsKey("child_states") ? dictionary2["child_states"].AsGodotDictionary() : dictionary2);
				}
				if (dictionary != null && TryGetLegacyActiveLeaf(dictionary, out var stateName2))
				{
					stateName = stateName2;
				}
				return true;
			}
		}
		return false;
	}
}
