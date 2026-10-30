using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewRelifeCustomPreviewRuntimeTest : Node
{
	private const string PacketPath = "res://Asset/Anime/Character/Plant/Diamond/Relife/Packet/PlantRelife.tres";

	private const string SpriteScenePath = "res://Asset/Anime/Character/Plant/Diamond/Relife/Relife.tscn";

	private const string CharacterScenePath = "res://Asset/Anime/Character/Plant/Diamond/Relife/Scene/TowerDefensePlantRelife.tscn";

	private const string PacketPreviewScenePath = "res://Prefab/TowerDefense/GUI/InGame/Packet/TowerDefenseInGamePacketShow.tscn";

	private const string PacketSaveKey = "PlantRelife";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		Resource previousSprite = null;
		bool hadPreviousSprite = false;
		Dictionary previousSave = null;
		string previousUser = null;
		TowerDefenseInGamePacketShow preview = null;
		TowerDefensePlantRelife character = null;
		try
		{
			_ = 6;
			try
			{
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(GameSaveManager.Instance), "GameSaveManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(BattleEventBus.Instance), "BattleEventBus autoload must be available.");
				if (!GodotObject.IsInstanceValid(ResourceManager.Instance) || !GodotObject.IsInstanceValid(GameSaveManager.Instance) || !GodotObject.IsInstanceValid(BattleEventBus.Instance))
				{
					goto end_IL_0098;
				}
				previousUser = GameSaveManager.Instance.GetUserCurrent();
				if (string.IsNullOrEmpty(previousUser))
				{
					GameSaveManager.Instance.SetUserCurrent("RelifePreviewRuntimeTest");
				}
				TowerDefensePacketConfig packet = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Diamond/Relife/Packet/PlantRelife.tres", null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Diamond/Relife/Relife.tscn", null, ResourceLoader.CacheMode.Ignore);
				PackedScene characterScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Diamond/Relife/Scene/TowerDefensePlantRelife.tscn", null, ResourceLoader.CacheMode.Ignore);
				PackedScene packedScene2 = ResourceLoader.Load<PackedScene>("res://Prefab/TowerDefense/GUI/InGame/Packet/TowerDefenseInGamePacketShow.tscn", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(packet) && packet.saveKey == "PlantRelife", "The regression must load the real Relife packet.");
				Check(GodotObject.IsInstanceValid(packedScene) && GodotObject.IsInstanceValid(characterScene) && GodotObject.IsInstanceValid(packedScene2), "The regression must load the real Relife sprite, character, and packet preview scenes.");
				if (!GodotObject.IsInstanceValid(packet) || !GodotObject.IsInstanceValid(packedScene) || !GodotObject.IsInstanceValid(characterScene) || !GodotObject.IsInstanceValid(packedScene2))
				{
					goto end_IL_0098;
				}
				hadPreviousSprite = ResourceManager.Instance.CHARCTAER_SPRITE.TryGetValue("PlantRelife", out previousSprite);
				ResourceManager.Instance.CHARCTAER_SPRITE["PlantRelife"] = packedScene;
				Dictionary towerDefensePacketValue = GameSaveManager.Instance.GetTowerDefensePacketValue("PlantRelife");
				previousSave = towerDefensePacketValue.Duplicate(deep: true);
				Dictionary dictionary = towerDefensePacketValue.GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary();
				dictionary["Custom"] = "Custom0";
				towerDefensePacketValue["Key"] = dictionary;
				GameSaveManager.Instance.SetTowerDefensePacketValue("PlantRelife", towerDefensePacketValue);
				string text = GameSaveManager.Instance.GetTowerDefensePacketValue("PlantRelife").GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary()
					.GetValueOrDefault("Custom", "")
					.AsString();
				Check(text == "Custom0", "The isolated save fixture must equip Relife Custom0 before creating the preview.");
				preview = packedScene2.Instantiate<TowerDefenseInGamePacketShow>(PackedScene.GenEditState.Disabled);
				AddChild(preview, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(2);
				preview.Init(packet);
				await WaitFrames(3);
				AdobeAnimateSprite previewRoot = preview.sprite;
				AdobeAnimateSprite previewBack = previewRoot?.FindChild("Back", recursive: true, owned: false) as AdobeAnimateSprite;
				Check(GodotObject.IsInstanceValid(previewRoot) && GodotObject.IsInstanceValid(previewBack), "The real packet preview must contain the Relife root and nested Back sprites.");
				Check(previewRoot.GetFliter("skin1_1"), "The equipped Custom0 must be applied to the packet preview root.");
				Check(previewBack.GetFliter("skin1_4") && !previewBack.GetFliter("Pumpkin_back"), "The initially equipped Custom0 must replace the nested pumpkin Back with skin1_4.");
				Check(previewRoot.GetFliter("图层_5"), "Relife Custom0 must retain the shared green bottom-leaf layer 图层_5.");
				BattleEventBus.Instance.EmitCharacterSkinSwitched("PlantRelife", "");
				await WaitFrames(2);
				Check(!previewRoot.GetFliter("skin1_1"), "Switching the packet preview to default must clear the root Custom0 layers.");
				Check(!previewBack.GetFliter("skin1_4") && previewBack.GetFliter("Pumpkin_back"), "Switching the packet preview to default must restore the nested pumpkin Back.");
				BattleEventBus.Instance.EmitCharacterSkinSwitched("PlantRelife", "Custom0");
				await WaitFrames(2);
				Check(previewRoot.GetFliter("skin1_1"), "Switching the existing packet preview back to Custom0 must refresh the root in place.");
				Check(previewBack.GetFliter("skin1_4") && !previewBack.GetFliter("Pumpkin_back"), "Switching the existing packet preview back to Custom0 must refresh the nested Back in place.");
				character = characterScene.Instantiate<TowerDefensePlantRelife>(PackedScene.GenEditState.Disabled);
				character.inGame = false;
				character.currentCustom = new Array<string> { "Custom0" };
				AddChild(character, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(3);
				RelifeSprite relifeSprite = character.sprite as RelifeSprite;
				AdobeAnimateSprite characterBack = relifeSprite?.FindChild("Back", recursive: true, owned: false) as AdobeAnimateSprite;
				Check(GodotObject.IsInstanceValid(relifeSprite) && GodotObject.IsInstanceValid(characterBack), "The real Relife character must retain its root and separate Back sprites.");
				Check(characterBack.GetFliter("skin1_4") && !characterBack.GetFliter("Pumpkin_back"), "The complete Relife character must still display the Custom0 Back.");
				character.SwitchCustom("");
				await WaitFrames(2);
				Check(!characterBack.GetFliter("skin1_4") && characterBack.GetFliter("Pumpkin_back"), "The complete Relife character must still restore its default Back.");
				character.SwitchCustom("Custom0");
				await WaitFrames(2);
				Check(characterBack.GetFliter("skin1_4") && !characterBack.GetFliter("Pumpkin_back"), "The complete Relife character must still switch back to Custom0.");
				goto end_IL_0071;
				end_IL_0098:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewRelifeCustomPreviewRuntimeTest] Unexpected exception: {value}");
				goto end_IL_0071;
			}
			return;
			end_IL_0071:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(preview))
			{
				preview.QueueFree();
			}
			if (GodotObject.IsInstanceValid(character))
			{
				character.QueueFree();
			}
			if (GodotObject.IsInstanceValid(ResourceManager.Instance))
			{
				if (hadPreviousSprite)
				{
					ResourceManager.Instance.CHARCTAER_SPRITE["PlantRelife"] = previousSprite;
				}
				else
				{
					ResourceManager.Instance.CHARCTAER_SPRITE.Remove("PlantRelife");
				}
			}
			if (GodotObject.IsInstanceValid(GameSaveManager.Instance) && previousSave != null)
			{
				GameSaveManager.Instance.SetTowerDefensePacketValue("PlantRelife", previousSave);
			}
			if (GodotObject.IsInstanceValid(GameSaveManager.Instance) && previousUser != null)
			{
				GameSaveManager.Instance.config.userCurrent = previousUser;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag = _failures == 0 && _checks == 18;
		GD.Print($"RELIFE_CUSTOM_PREVIEW_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
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
			GD.PushError("[BugOverviewRelifeCustomPreviewRuntimeTest] " + message);
		}
	}
}
