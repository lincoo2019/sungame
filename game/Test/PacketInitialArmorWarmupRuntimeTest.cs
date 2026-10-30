using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class PacketInitialArmorWarmupRuntimeTest : Node
{
	private const string ResultMarker = "PACKET_INITIAL_ARMOR_RESULT";

	private readonly List<string> _failures = new List<string>();

	private int _checks;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		PacketInitialArmorWarmupControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		List<TowerDefenseZombieNormal> spawnedZombies = new List<TowerDefenseZombieNormal>();
		try
		{
			_ = 5;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Require(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload is unavailable.");
				ResourceManager resources = ResourceManager.Instance;
				Require(GodotObject.IsInstanceValid(resources), "ResourceManager autoload is unavailable.");
				resources.BeginLoad();
				for (int frame = 0; frame < 7200; frame++)
				{
					if (resources.CurrentGameplayResourceLoadState == GameplayResourceLoadState.Ready)
					{
						break;
					}
					if (resources.CurrentGameplayResourceLoadState == GameplayResourceLoadState.Failed)
					{
						break;
					}
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				}
				Check(resources.AreFullGameplayResourcesReady, $"Full gameplay resources must be ready before armor spawning: state={resources.CurrentGameplayResourceLoadState}, error={resources.FullGameplayResourceLoadError}");
				Require(resources.AreFullGameplayResourcesReady, "Full gameplay resources did not become ready.");
				int lateLoadCountBefore = resources.LateCharacterResourceLoadCount;
				control = new PacketInitialArmorWarmupControlStub
				{
					Name = "PacketInitialArmorWarmupControl",
					isGameRunning = false,
					isInit = true
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.gridBeginPos = Vector2.Zero;
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 1);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateGroundMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				manager.currentControl = control;
				PackedScene characterScene = resources.GetCharacterScene("ZombieNormal");
				TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieNormalCone");
				TowerDefensePacketConfig packetConfig2 = TowerDefenseManager.GetPacketConfig("ZombieNormalBlackHelmet");
				Check(GodotObject.IsInstanceValid(characterScene) && GodotObject.IsInstanceValid(packetConfig) && GodotObject.IsInstanceValid(packetConfig2), "The production normal-zombie scene and armor packets must already be resident.");
				Require(GodotObject.IsInstanceValid(characterScene) && GodotObject.IsInstanceValid(packetConfig) && GodotObject.IsInstanceValid(packetConfig2), "The production scene or armor packets are unavailable.");
				TowerDefensePacketConfig[] packets = new TowerDefensePacketConfig[2] { packetConfig, packetConfig2 };
				string[] armorNames = new string[2] { "Cone", "BlackHelmet" };
				string[] saveKeys = new string[2] { "ZombieNormalCone", "ZombieNormalBlackHelmet" };
				for (int frame = 0; frame < packets.Length; frame++)
				{
					TowerDefensePacketConfig packet = packets[frame];
					string armorName = armorNames[frame];
					Check(packet.saveKey == saveKeys[frame] && packet.characterConfig?.name == "ZombieNormal" && packet.initArmor.Count == 1 && packet.initArmor[0] == armorName, "The fixture must use the authored normal-zombie " + armorName + " packet.");
					TowerDefenseZombieNormal zombie = packet.Spawn(1, (double)frame * 120.0, isIdle: true) as TowerDefenseZombieNormal;
					Check(GodotObject.IsInstanceValid(zombie), "The " + armorName + " packet must spawn a normal zombie.");
					Require(GodotObject.IsInstanceValid(zombie), "The " + armorName + " packet did not create a zombie.");
					spawnedZombies.Add(zombie);
					await WaitFrames(3);
					Check(zombie.IsNodeReady() && zombie.inGame && GodotObject.IsInstanceValid(zombie.instance), "The " + armorName + " zombie must finish normal fresh-scene initialization.");
					Check(zombie.currentArmor.Contains(armorName), "The packet " + armorName + " key must remain in currentArmor after spawning.");
					Check(zombie.instance.ArmorHas(armorName), "The packet must create the logical " + armorName + " armor instance.");
					TowerDefenseArmorInstance armor = FindArmor(zombie, armorName);
					Check(GodotObject.IsInstanceValid(armor) && armor.hitPoints > 0.0 && armor.hitpointsSave > 0.0, "The packet " + armorName + " must have a live damageable armor instance.");
					Check(GodotObject.IsInstanceValid(armor?.sprite) && !string.IsNullOrWhiteSpace(armor.sprite.externalAtlasTexturePath), "The packet " + armorName + " must create its authored shared-atlas part.");
					AdobeAnimateSlot armorSlot = zombie.sprite.GetNodeOrNull<AdobeAnimateSlot>(armor?.slotConfig?.slotPath ?? null);
					Check(GodotObject.IsInstanceValid(armorSlot) && armor?.sprite?.GetParent() == armorSlot, "The " + armorName + " atlas part must attach to its authored animation slot.");
					AdobeAnimatePart originalArmorVisual = armor.sprite;
					string expectedTexturePath = armor.sprite.externalAtlasTexturePath;
					double bodyHitpointsBefore = zombie.instance.hitpoints;
					armor.Hurt(armor.hitPoints, playSplatAudio: false, new Vector2(-120f, -260f), createDamagePart: true, ignoreLimit: true);
					await WaitFrames(2);
					Check(!zombie.instance.ArmorHas(armorName) && armor.isRemove, "Broken " + armorName + " armor must leave every logical armor index.");
					Check(Mathf.IsEqualApprox(zombie.instance.hitpoints, bodyHitpointsBefore), "Breaking " + armorName + " directly must not damage the zombie body.");
					bool flag = !GodotObject.IsInstanceValid(originalArmorVisual) || originalArmorVisual.GetParent() != armorSlot;
					Check(!GodotObject.IsInstanceValid(armor.sprite) && flag, "Broken " + armorName + " must remove its original visual from the zombie animation slot.");
					DamagePartDrop damagePartDrop = FindActiveArmorDrop(control.characterNode, expectedTexturePath);
					Check(GodotObject.IsInstanceValid(damagePartDrop), "Broken " + armorName + " must create a live DamagePartDrop node.");
					Check(damagePartDrop?.GetParent() == zombie.GetParent() && damagePartDrop?.GetCanvas() == zombie.GetCanvas(), "The " + armorName + " drop must stay in the zombie character canvas.");
					bool flag2 = damagePartDrop?.sprite is AdobeAnimatePart adobeAnimatePart && adobeAnimatePart != originalArmorVisual && adobeAnimatePart.externalAtlasTexturePath == expectedTexturePath;
					Check(flag2, "The " + armorName + " drop node must use an independent copy with its authored texture instead of a white block.");
					int num = Mathf.Clamp(zombie.gridPos.Y * 15 + 8, -4096, 4096);
					Check(damagePartDrop != null && damagePartDrop.ZIndex == num, "The " + armorName + " drop must use the row-based DAMAGEPART Z bucket.");
					GD.Print($"[PacketArmorNodeDrop] armor={armorName} texture={expectedTexturePath} z={damagePartDrop?.ZIndex} originalDetached={flag} independentCopy={flag2}");
					zombie.ActivateLevelEntryPreview(packet.packetAnimeClip);
					double previewPhaseStart = (double)zombie.sprite.frameIndex + zombie.sprite.elapsedTimer;
					Vector2 previewPosition = zombie.GlobalPosition;
					await WaitFrames(18);
					double num2 = (double)zombie.sprite.frameIndex + zombie.sprite.elapsedTimer;
					Check(zombie.isShow && zombie.ProcessMode == ProcessModeEnum.Disabled && zombie.sprite.ProcessMode == ProcessModeEnum.Always, "The " + armorName + " level-entry preview must disable gameplay while keeping the body animation independently processable.");
					Check(!zombie.sprite.pause && zombie.sprite.RuntimeManagerDispatchActive, "The " + armorName + " level-entry body must enter Adobe Animate dispatch without waiting for a pause-menu round trip.");
					Check(Math.Abs(num2 - previewPhaseStart) > 0.001, $"The {armorName} level-entry body animation must advance; start={previewPhaseStart:F4}, end={num2:F4}.");
					Check(zombie.GlobalPosition.IsEqualApprox(previewPosition), "The " + armorName + " level-entry preview must remain stationary while its body animation advances.");
					double previewPhaseBeforePause = (double)zombie.sprite.frameIndex + zombie.sprite.elapsedTimer;
					GetTree().Paused = true;
					for (int pauseFrame = 0; pauseFrame < 8; pauseFrame++)
					{
						await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
					}
					double previewPhaseDuringPause = (double)zombie.sprite.frameIndex + zombie.sprite.elapsedTimer;
					GetTree().Paused = false;
					await WaitFrames(4);
					Check(Math.Abs(previewPhaseDuringPause - previewPhaseBeforePause) > 0.001 && zombie.sprite.RuntimeManagerDispatchActive, $"The {armorName} level-entry body must keep advancing through pause and resume; before={previewPhaseBeforePause:F4}, paused={previewPhaseDuringPause:F4}.");
				}
				Check(resources.LateCharacterResourceLoadCount == lateLoadCountBefore, $"Initial armor and preview runtime triggered a late character resource load: before={lateLoadCountBefore}, after={resources.LateCharacterResourceLoadCount}");
			}
			catch (Exception value)
			{
				_failures.Add($"Unexpected runtime exception: {value}");
			}
		}
		finally
		{
			GetTree().Paused = false;
			foreach (TowerDefenseZombieNormal item in spawnedZombies)
			{
				if (GodotObject.IsInstanceValid(item) && !item.IsQueuedForDeletion())
				{
					item.QueueFree();
				}
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				if (GodotObject.IsInstanceValid(control))
				{
					manager.ReleaseBattleReferences(control, preserveLevelConfig: true);
				}
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
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			await WaitFrames(6);
			ObjectManager.Instance?.Clear();
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			await WaitFrames(3);
		}
		foreach (string failure in _failures)
		{
			GD.PushError("[PacketInitialArmorWarmupRuntimeTest] " + failure);
		}
		bool flag3 = _failures.Count == 0 && _checks == 44;
		GD.Print($"{"PACKET_INITIAL_ARMOR_RESULT"} passed={flag3} checks={_checks} failures={_failures.Count}");
		GetTree().Quit((!flag3) ? 2 : 0);
	}

	private static TowerDefenseArmorInstance FindArmor(TowerDefenseCharacter character, string armorName)
	{
		if (character?.instance?.armorList == null)
		{
			return null;
		}
		foreach (TowerDefenseArmorInstance armor in character.instance.armorList)
		{
			if (armor?.slotConfig?.armorName == armorName && !armor.isRemove)
			{
				return armor;
			}
		}
		return null;
	}

	private static DamagePartDrop FindActiveArmorDrop(Node characterNode, string texturePath)
	{
		if (!GodotObject.IsInstanceValid(characterNode) || string.IsNullOrWhiteSpace(texturePath))
		{
			return null;
		}
		foreach (Node child in characterNode.GetChildren())
		{
			if (child is DamagePartDrop { over: false, sprite: AdobeAnimatePart sprite } damagePartDrop && sprite.externalAtlasTexturePath == texturePath)
			{
				return damagePartDrop;
			}
		}
		return null;
	}

	private static TowerDefenseBattleFeatureMap CreateGroundMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig
			{
				gridNum = gridNum,
				gridSize = new Vector2(100f, 76f),
				gridBeginPos = Vector2.Zero
			}
		});
		towerDefenseBattleFeatureMap.PlantGridInit();
		for (int i = 1; i <= gridNum.X; i++)
		{
			for (int j = 1; j <= gridNum.Y; j++)
			{
				TowerDefenseCellConfig towerDefenseCellConfig = new TowerDefenseCellConfig();
				towerDefenseCellConfig.gridType.Add(TowerDefenseEnum.PLANTGRIDTYPE.GROUND);
				towerDefenseBattleFeatureMap.GetPlantGridCell(new Vector2I(i, j))?.Init(towerDefenseCellConfig);
			}
		}
		return towerDefenseBattleFeatureMap;
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}

	private async Task WaitFrames(int count)
	{
		for (int index = 0; index < count; index++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures.Add(message);
		}
	}
}
