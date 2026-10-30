using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugDepartmentLevelEditorPacketBackgroundRuntimeTest : Node
{
	private const string WaveChooseScenePath = "res://Prefab/GUI/LevelEditor/WaveEditor/PacketChoose/LevelEditorWaveEditorPacketChoose.tscn";

	private const string SeedChooseScenePath = "res://Prefab/GUI/LevelEditor/SeedbankEditor/Choose/LevelEditorSeedBankChoose.tscn";

	private const string PacketBankResourcePath = "res://Asset/Config/PacketBank/PacketBankResource.json";

	private const string ZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string WhitePacketPath = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres";

	private const string ZombieCharacterScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string WhiteCharacterScenePath = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Scene/TowerDefensePlantSunFlower.tscn";

	private const string ZombieSpriteUid = "uid://vp5mpqwb0n8b";

	private const string WhiteSpriteUid = "uid://cr8mehnwhnl36";

	private const string ZombiePacketTextureUid = "uid://btgdkkg66xc8d";

	private const string WhitePacketTextureUid = "uid://bwksngvkn16cd";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		ResourceManager resources = ResourceManager.Instance;
		LevelEditorWaveEditorPacketChoose waveChoose = null;
		LevelEditorSeedBankChoose seedChoose = null;
		Resource previousWavePacket = null;
		Resource previousSeedPacket = null;
		Resource previousZombieCharacter = null;
		Resource previousWhiteCharacter = null;
		Resource previousZombieSprite = null;
		Resource previousWhiteSprite = null;
		TowerDefensePacketBankData previousTotalBank = null;
		bool hadPreviousWavePacket = false;
		bool hadPreviousSeedPacket = false;
		bool hadPreviousZombieCharacter = false;
		bool hadPreviousWhiteCharacter = false;
		bool hadPreviousZombieSprite = false;
		bool hadPreviousWhiteSprite = false;
		bool hadPreviousTotalBank = false;
		string zombiePacketKey = "ZombieNormal";
		string whitePacketKey = "PlantSunFlower";
		try
		{
			_ = 3;
			try
			{
				Check(GodotObject.IsInstanceValid(resources), "ResourceManager autoload must be available.");
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Prefab/GUI/LevelEditor/WaveEditor/PacketChoose/LevelEditorWaveEditorPacketChoose.tscn", null, ResourceLoader.CacheMode.Ignore);
				PackedScene packedScene2 = ResourceLoader.Load<PackedScene>("res://Prefab/GUI/LevelEditor/SeedbankEditor/Choose/LevelEditorSeedBankChoose.tscn", null, ResourceLoader.CacheMode.Ignore);
				Texture2D expectedZombieTexture = ResourceLoader.Load<Texture2D>("uid://btgdkkg66xc8d", null, ResourceLoader.CacheMode.Reuse);
				Texture2D expectedWhiteTexture = ResourceLoader.Load<Texture2D>("uid://bwksngvkn16cd", null, ResourceLoader.CacheMode.Reuse);
				TowerDefensePacketConfig waveConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres", null, ResourceLoader.CacheMode.Ignore);
				TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres", null, ResourceLoader.CacheMode.Ignore);
				PackedScene packedScene3 = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.Ignore);
				PackedScene packedScene4 = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Scene/TowerDefensePlantSunFlower.tscn", null, ResourceLoader.CacheMode.Ignore);
				PackedScene packedScene5 = ResourceLoader.Load<PackedScene>("uid://vp5mpqwb0n8b", null, ResourceLoader.CacheMode.Reuse);
				PackedScene packedScene6 = ResourceLoader.Load<PackedScene>("uid://cr8mehnwhnl36", null, ResourceLoader.CacheMode.Reuse);
				TowerDefensePacketBankData towerDefensePacketBankData = LoadAuthoredTotalPacketBank();
				Check(GodotObject.IsInstanceValid(packedScene) && GodotObject.IsInstanceValid(packedScene2), "Both production level-editor packet chooser scenes must load.");
				Check(GodotObject.IsInstanceValid(expectedZombieTexture) && GodotObject.IsInstanceValid(expectedWhiteTexture), "The authored desktop Zombie and White packet backgrounds must load.");
				Check(GodotObject.IsInstanceValid(waveConfig) && GodotObject.IsInstanceValid(towerDefensePacketConfig), "The real ZombieNormal and PlantSunFlower packet resources must load.");
				Check(GodotObject.IsInstanceValid(waveConfig?.characterConfig) && GodotObject.IsInstanceValid(towerDefensePacketConfig?.characterConfig), "Both real packets must retain their authored characterConfig resources.");
				Check(waveConfig != null && waveConfig._GetType() == TowerDefenseEnum.PACKET_TYPE.ZOMBIE && towerDefensePacketConfig != null && towerDefensePacketConfig._GetType() == TowerDefenseEnum.PACKET_TYPE.WHITE, "ZombieNormal must be a Zombie packet and PlantSunFlower must be a White packet.");
				Check(waveConfig?.saveKey == "ZombieNormal" && towerDefensePacketConfig?.saveKey == "PlantSunFlower" && waveConfig?.characterConfig?.name == "ZombieNormal" && towerDefensePacketConfig?.characterConfig?.name == "PlantSunFlower", "The scenario must use the production packet saveKeys and character identities.");
				Check(GodotObject.IsInstanceValid(packedScene3) && GodotObject.IsInstanceValid(packedScene4) && GodotObject.IsInstanceValid(packedScene5) && GodotObject.IsInstanceValid(packedScene6), "The real ZombieNormal and PlantSunFlower character and packet-preview scenes must load.");
				Check(GodotObject.IsInstanceValid(towerDefensePacketBankData) && towerDefensePacketBankData.GetCategory("White").Contains("PlantWallnutSquashBowling"), "The real authored Total packet bank must load before either chooser enters the tree.");
				zombiePacketKey = waveConfig.saveKey;
				whitePacketKey = towerDefensePacketConfig.saveKey;
				hadPreviousWavePacket = resources.TOWERDEFENSE_PACKETS.TryGetValue(zombiePacketKey, out previousWavePacket);
				hadPreviousSeedPacket = resources.TOWERDEFENSE_PACKETS.TryGetValue(whitePacketKey, out previousSeedPacket);
				hadPreviousZombieCharacter = resources.TOWERDEFENSE_CHARCATERS.TryGetValue(waveConfig.characterConfig.name, out previousZombieCharacter);
				hadPreviousWhiteCharacter = resources.TOWERDEFENSE_CHARCATERS.TryGetValue(towerDefensePacketConfig.characterConfig.name, out previousWhiteCharacter);
				hadPreviousZombieSprite = resources.CHARCTAER_SPRITE.TryGetValue(waveConfig.characterConfig.name, out previousZombieSprite);
				hadPreviousWhiteSprite = resources.CHARCTAER_SPRITE.TryGetValue(towerDefensePacketConfig.characterConfig.name, out previousWhiteSprite);
				hadPreviousTotalBank = resources.TOWERDEFENSE_PACKETBANKS.TryGetValue("Total", out previousTotalBank);
				resources.TOWERDEFENSE_PACKETS[zombiePacketKey] = waveConfig;
				resources.TOWERDEFENSE_PACKETS[whitePacketKey] = towerDefensePacketConfig;
				resources.TOWERDEFENSE_CHARCATERS[waveConfig.characterConfig.name] = packedScene3;
				resources.TOWERDEFENSE_CHARCATERS[towerDefensePacketConfig.characterConfig.name] = packedScene4;
				resources.CHARCTAER_SPRITE[waveConfig.characterConfig.name] = packedScene5;
				resources.CHARCTAER_SPRITE[towerDefensePacketConfig.characterConfig.name] = packedScene6;
				resources.TOWERDEFENSE_PACKETBANKS["Total"] = towerDefensePacketBankData;
				waveChoose = packedScene.Instantiate<LevelEditorWaveEditorPacketChoose>(PackedScene.GenEditState.Disabled);
				seedChoose = packedScene2.Instantiate<LevelEditorSeedBankChoose>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(waveChoose), "The real WaveEditor packet chooser must instantiate.");
				Check(GodotObject.IsInstanceValid(seedChoose), "The real SeedBank packet chooser must instantiate.");
				if (!GodotObject.IsInstanceValid(waveChoose) || !GodotObject.IsInstanceValid(seedChoose))
				{
					throw new InvalidOperationException("Production packet chooser fixtures are unavailable.");
				}
				waveChoose.Visible = false;
				seedChoose.Visible = false;
				AddChild(waveChoose, forceReadableName: false, InternalMode.Disabled);
				AddChild(seedChoose, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(1);
				Check(waveChoose.IsNodeReady(), "The real WaveEditor packet chooser must complete _Ready.");
				Check(seedChoose.IsNodeReady(), "The real SeedBank packet chooser must complete _Ready.");
				GridContainer waveContainer = waveChoose.GetNodeOrNull<GridContainer>("%PacketContainer");
				GridContainer nodeOrNull = seedChoose.GetNodeOrNull<GridContainer>("%PacketContainer");
				Check(GodotObject.IsInstanceValid(waveContainer) && GodotObject.IsInstanceValid(nodeOrNull), "Both real editor choosers must expose their authored PacketContainer.");
				TowerDefenseInGamePacketShow preReadyPacket = TowerDefenseManager.CreatePacketShowWithConfig(waveConfig);
				Check(GodotObject.IsInstanceValid(preReadyPacket), "CreatePacketShowWithConfig must instantiate the production packet card.");
				Check(!preReadyPacket.IsNodeReady() && preReadyPacket.backgroundTexture == null, "The regression must initialize the packet before _Ready binds BackgroundTexture.");
				Check(preReadyPacket.config == waveConfig, "Pre-_Ready Init must retain the requested Zombie packet config without throwing.");
				Check(preReadyPacket.backgroundTexture == null, "Pre-_Ready Init must defer the background write until the real control exists.");
				preReadyPacket.setPcLayout = true;
				waveContainer.AddChild(preReadyPacket, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(1);
				Check(preReadyPacket.IsNodeReady(), "The pre-initialized packet must subsequently complete its real _Ready path.");
				Check(preReadyPacket.GetParent() == waveContainer, "The pre-initialized packet must mount in the real WaveEditor PacketContainer.");
				Check(GodotObject.IsInstanceValid(preReadyPacket.backgroundTexture), "The production packet scene must bind BackgroundTexture during _Ready.");
				Check(TextureMatches(preReadyPacket.backgroundTexture.Texture, expectedZombieTexture), "Deferred _Ready application must refresh the pre-initialized card to the Zombie background.");
				Check(GodotObject.IsInstanceValid(preReadyPacket.coldDownProgressBar) && Mathf.IsEqualApprox(preReadyPacket.coldDownProgressBar.MaxValue, preReadyPacket.coldDown), "Deferred _Ready application must also restore the initialized cooldown UI state.");
				Check(GodotObject.IsInstanceValid(preReadyPacket.sprite) && preReadyPacket.sprite.GetParent() == preReadyPacket.previewSpriteNode, "Deferred _Ready application must create and mount the real ZombieNormal preview.");
				int guardedPressCount = 0;
				preReadyPacket.OnPressed += delegate
				{
					guardedPressCount++;
				};
				preReadyPacket.itemCost = 9223372036854775807L;
				preReadyPacket.alive = true;
				preReadyPacket.button.EmitSignal(BaseButton.SignalName.Pressed);
				Check(guardedPressCount == 0 && !preReadyPacket.alive, "Runtime-enforced packet clicks must still reject an unaffordable stale card.");
				TowerDefensePacketBankData towerDefensePacketBankData2 = new TowerDefensePacketBankData();
				towerDefensePacketBankData2.category["Zombie"] = new Array<string> { zombiePacketKey };
				waveChoose.data = towerDefensePacketBankData2;
				waveChoose.CategoryChoose("Zombie", reFresh: true);
				await WaitFrames(1);
				Check(waveChoose.packetList.Count == 1, "WaveEditor CategoryChoose must create exactly one focused real packet card.");
				TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = ((waveChoose.packetList.Count == 1) ? waveChoose.packetList[0] : null);
				Check(GodotObject.IsInstanceValid(towerDefenseInGamePacketShow) && towerDefenseInGamePacketShow.IsNodeReady(), "WaveEditor CategoryChoose must initialize its packet after normal _Ready.");
				Check(towerDefenseInGamePacketShow != null && towerDefenseInGamePacketShow.config?._GetType() == TowerDefenseEnum.PACKET_TYPE.ZOMBIE, "WaveEditor CategoryChoose must retain the requested Zombie config.");
				Check(GodotObject.IsInstanceValid(towerDefenseInGamePacketShow?.backgroundTexture) && TextureMatches(towerDefenseInGamePacketShow.backgroundTexture.Texture, expectedZombieTexture), "WaveEditor CategoryChoose must apply the authored Zombie background.");
				Check(GodotObject.IsInstanceValid(towerDefenseInGamePacketShow?.sprite) && towerDefenseInGamePacketShow.sprite.GetParent() == towerDefenseInGamePacketShow.previewSpriteNode, "WaveEditor CategoryChoose must mount the real ZombieNormal packet preview.");
				int wavePressCount = 0;
				towerDefenseInGamePacketShow.ClearEventHandlers();
				towerDefenseInGamePacketShow.OnPressed += delegate
				{
					wavePressCount++;
				};
				towerDefenseInGamePacketShow.itemCost = 9223372036854775807L;
				towerDefenseInGamePacketShow.alive = true;
				towerDefenseInGamePacketShow.button.EmitSignal(BaseButton.SignalName.Pressed);
				Check(!towerDefenseInGamePacketShow.enforceRuntimeAvailabilityOnPress, "WaveEditor browser cards must opt out of battle-only availability checks.");
				Check(wavePressCount == 1, "WaveEditor browser card clicks must reach their selection callback regardless of Sun.");
				TowerDefensePacketBankData towerDefensePacketBankData3 = new TowerDefensePacketBankData();
				towerDefensePacketBankData3.category["White"] = new Array<string> { whitePacketKey };
				seedChoose.data = towerDefensePacketBankData3;
				seedChoose.CategoryChoose("White", reFresh: true);
				await WaitFrames(1);
				Check(seedChoose.packetList.Count == 1, "SeedBank CategoryChoose must create exactly one focused real packet card.");
				TowerDefenseInGamePacketShow towerDefenseInGamePacketShow2 = ((seedChoose.packetList.Count == 1) ? seedChoose.packetList[0] : null);
				Check(GodotObject.IsInstanceValid(towerDefenseInGamePacketShow2) && towerDefenseInGamePacketShow2.IsNodeReady(), "SeedBank CategoryChoose must initialize its packet after normal _Ready.");
				Check(towerDefenseInGamePacketShow2 != null && towerDefenseInGamePacketShow2.config?._GetType() == TowerDefenseEnum.PACKET_TYPE.WHITE, "SeedBank CategoryChoose must retain the requested White config.");
				Check(GodotObject.IsInstanceValid(towerDefenseInGamePacketShow2?.backgroundTexture) && TextureMatches(towerDefenseInGamePacketShow2.backgroundTexture.Texture, expectedWhiteTexture), "SeedBank CategoryChoose must apply the authored White background.");
				Check(GodotObject.IsInstanceValid(towerDefenseInGamePacketShow2?.sprite) && towerDefenseInGamePacketShow2.sprite.GetParent() == towerDefenseInGamePacketShow2.previewSpriteNode, "SeedBank CategoryChoose must mount the real PlantSunFlower packet preview.");
				int seedPressCount = 0;
				towerDefenseInGamePacketShow2.ClearEventHandlers();
				towerDefenseInGamePacketShow2.OnPressed += delegate
				{
					seedPressCount++;
				};
				towerDefenseInGamePacketShow2.itemCost = 9223372036854775807L;
				towerDefenseInGamePacketShow2.alive = true;
				towerDefenseInGamePacketShow2.button.EmitSignal(BaseButton.SignalName.Pressed);
				Check(!towerDefenseInGamePacketShow2.enforceRuntimeAvailabilityOnPress, "SeedBank browser cards must opt out of battle-only availability checks.");
				Check(seedPressCount == 1, "SeedBank browser card clicks must reach their selection callback regardless of Sun.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugDepartmentLevelEditorPacketBackgroundRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(resources))
			{
				RestoreResource(resources.TOWERDEFENSE_PACKETS, zombiePacketKey, hadPreviousWavePacket, previousWavePacket);
				RestoreResource(resources.TOWERDEFENSE_PACKETS, whitePacketKey, hadPreviousSeedPacket, previousSeedPacket);
				RestoreResource(resources.TOWERDEFENSE_CHARCATERS, "ZombieNormal", hadPreviousZombieCharacter, previousZombieCharacter);
				RestoreResource(resources.TOWERDEFENSE_CHARCATERS, "PlantSunFlower", hadPreviousWhiteCharacter, previousWhiteCharacter);
				RestoreResource(resources.CHARCTAER_SPRITE, "ZombieNormal", hadPreviousZombieSprite, previousZombieSprite);
				RestoreResource(resources.CHARCTAER_SPRITE, "PlantSunFlower", hadPreviousWhiteSprite, previousWhiteSprite);
				if (hadPreviousTotalBank)
				{
					resources.TOWERDEFENSE_PACKETBANKS["Total"] = previousTotalBank;
				}
				else
				{
					resources.TOWERDEFENSE_PACKETBANKS.Remove("Total");
				}
			}
			if (GodotObject.IsInstanceValid(waveChoose))
			{
				waveChoose.QueueFree();
			}
			if (GodotObject.IsInstanceValid(seedChoose))
			{
				seedChoose.QueueFree();
			}
			await WaitFrames(2);
		}
		bool flag = _failures == 0 && _checks == 39;
		GD.Print($"LEVEL_EDITOR_PACKET_BACKGROUND_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static bool TextureMatches(Texture2D actual, Texture2D expected)
	{
		if (!GodotObject.IsInstanceValid(actual) || !GodotObject.IsInstanceValid(expected))
		{
			return false;
		}
		if (actual != expected && !(actual.GetRid() == expected.GetRid()))
		{
			if (!string.IsNullOrWhiteSpace(actual.ResourcePath))
			{
				return actual.ResourcePath == expected.ResourcePath;
			}
			return false;
		}
		return true;
	}

	private static TowerDefensePacketBankData LoadAuthoredTotalPacketBank()
	{
		Json json = ResourceLoader.Load<Json>("res://Asset/Config/PacketBank/PacketBankResource.json", null, ResourceLoader.CacheMode.Ignore);
		if (!GodotObject.IsInstanceValid(json) || json.Data.VariantType != Variant.Type.Dictionary)
		{
			return null;
		}
		if (!json.Data.AsGodotDictionary().TryGetValue("Total", out var value) || value.VariantType != Variant.Type.Dictionary)
		{
			return null;
		}
		if (!value.AsGodotDictionary().TryGetValue("Category", out var value2) || value2.VariantType != Variant.Type.Dictionary)
		{
			return null;
		}
		return new TowerDefensePacketBankData
		{
			category = value2.AsGodotDictionary().Duplicate(deep: true)
		};
	}

	private static void RestoreResource(System.Collections.Generic.Dictionary<string, Resource> resources, string key, bool hadPrevious, Resource previous)
	{
		if (hadPrevious)
		{
			resources[key] = previous;
		}
		else
		{
			resources.Remove(key);
		}
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
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
			_failures++;
			GD.PushError("[BugDepartmentLevelEditorPacketBackgroundRuntimeTest] " + message);
		}
	}
}
