using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewNewspaperArmorAngryVisualRuntimeTest : Node
{
	private const string NewspaperScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Paper/Scene/TowerDefenseZombiePaper.tscn";

	private const string ArmoredPacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Paper/Packet/ZombiePaperCone.tres";

	private const string AngryHeadReference = "uid://s4hnj2igecma";

	private const string ResultMarker = "BUG_OVERVIEW_NEWSPAPER_ARMOR_ANGRY_VISUAL_RESULT";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		BugOverviewNewspaperArmorAngryVisualControlStub control = null;
		TowerDefenseZombiePaper zombie = null;
		try
		{
			_ = 5;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Require(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload is unavailable.");
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Paper/Scene/TowerDefenseZombiePaper.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
				TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Zombie/Chapter1/Paper/Packet/ZombiePaperCone.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
				Check(GodotObject.IsInstanceValid(packedScene) && packedScene.CanInstantiate() && GodotObject.IsInstanceValid(towerDefensePacketConfig), "The real Newspaper Zombie scene and armored packet must load.");
				Require(GodotObject.IsInstanceValid(packedScene) && packedScene.CanInstantiate() && GodotObject.IsInstanceValid(towerDefensePacketConfig), "One or more authored Newspaper Zombie resources failed to load.");
				Check(towerDefensePacketConfig.saveKey == "ZombiePaperCone" && towerDefensePacketConfig.characterConfig?.name == "ZombiePaper" && towerDefensePacketConfig.initArmor.Count == 1 && towerDefensePacketConfig.initArmor[0] == "Cone", "The real armored packet must author Cone armor for ZombiePaper.");
				control = new BugOverviewNewspaperArmorAngryVisualControlStub
				{
					Name = "NewspaperArmorAngryVisualControl",
					isGameRunning = true,
					isInit = true
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				zombie = packedScene.Instantiate<TowerDefenseZombiePaper>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(zombie) && zombie.config?.name == "ZombiePaper" && zombie.SceneFilePath == "res://Asset/Anime/Character/Zombie/Chapter1/Paper/Scene/TowerDefenseZombiePaper.tscn", "The fixture must instantiate the reported real Newspaper Zombie scene.");
				Require(GodotObject.IsInstanceValid(zombie), "The Newspaper Zombie scene failed to instantiate.");
				foreach (string item in towerDefensePacketConfig.initArmor)
				{
					zombie.currentArmor.Add(item);
				}
				Check(zombie.currentArmor.Contains("Paper") && zombie.currentArmor.Contains("Cone"), "The character must enter the tree with both its newspaper and packet armor.");
				zombie.inGame = true;
				zombie.editorPreviewMode = false;
				zombie.ProcessMode = ProcessModeEnum.Disabled;
				control.characterNode.AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(4);
				Check(GodotObject.IsInstanceValid(zombie.instance) && GodotObject.IsInstanceValid(zombie.sprite) && GodotObject.IsInstanceValid(zombie.headSlot), "The real character instance, Adobe Animate body, and HeadSlot must initialize.");
				Require(GodotObject.IsInstanceValid(zombie.instance) && GodotObject.IsInstanceValid(zombie.sprite) && GodotObject.IsInstanceValid(zombie.headSlot), "The Newspaper Zombie runtime did not initialize.");
				zombie.WalkEntered();
				zombie.WalkExited();
				await WaitFrames(8);
				BugOverviewNewspaperArmorAngryVisualRuntimeTest bugOverviewNewspaperArmorAngryVisualRuntimeTest = this;
				GroundMoveComponent groundMoveComponent = zombie.groundMoveComponent;
				bugOverviewNewspaperArmorAngryVisualRuntimeTest.Check(groundMoveComponent != null && !groundMoveComponent.Alive, "被破报过渡打断的旧行走计时器不得再次开启地面位移。");
				TowerDefenseArmorInstance towerDefenseArmorInstance = FindArmor(zombie, "Paper");
				TowerDefenseArmorInstance cone = FindArmor(zombie, "Cone");
				Check(towerDefenseArmorInstance != null && cone != null && zombie.instance.ArmorHas("Paper") && zombie.instance.ArmorHas("Cone"), "Both real armor instances must be active before the newspaper tears.");
				Require(towerDefenseArmorInstance != null && cone != null, "The real Paper or Cone armor instance is missing.");
				AdobeAnimatePart conePart = cone.sprite;
				string coneTexturePath = ((GodotObject.IsInstanceValid(cone.typeData) && cone.typeData.stageAnimeTexturePaths.Count > 0) ? cone.typeData.stageAnimeTexturePaths[0] : string.Empty);
				Check(GodotObject.IsInstanceValid(conePart) && conePart.GetParent() == zombie.headSlot && conePart.externalAtlasTexturePath == coneTexturePath, "The packet-authored Cone must use its shared-atlas part on HeadSlot.");
				Check(zombie.headSlot.drawLayerId == -2, $"Newspaper HeadSlot must retain the authored Top layer; actual={zombie.headSlot.drawLayerId}.");
				Check(conePart.IsInsideTree() && AdobeAnimateManagedSprite2D.GetLogicalVisible(conePart), "The real Cone shared-atlas part must remain logically visible in HeadSlot.");
				Check(conePart.externalAtlasCentered, "The Cone shared-atlas part must retain Sprite2D-compatible centering.");
				towerDefenseArmorInstance.Hurt(towerDefenseArmorInstance.hitPoints, playSplatAudio: false, default(Vector2), createDamagePart: false);
				await WaitFrames(2);
				Check(!zombie.instance.ArmorHas("Paper") && zombie.CurrentStateHandle?.StableId == "zombie.paper.gasp" && zombie.headSlot.followSlotId == 18 && zombie.sprite.clip == "Gasp", $"Breaking Paper must enter Gasp on its live head layer; state={zombie.CurrentStateHandle?.StableId}, follow={zombie.headSlot.followSlotId}, clip={zombie.sprite.clip}.");
				Check(zombie.instance.ArmorHas("Cone") && FindArmor(zombie, "Cone") == cone, "Breaking Paper must not remove or recreate the packet-authored Cone.");
				Check(GodotObject.IsInstanceValid(conePart) && conePart.GetParent() == zombie.headSlot && conePart.externalAtlasTexturePath == coneTexturePath && AdobeAnimateManagedSprite2D.GetLogicalVisible(conePart), "The Cone part and atlas path must remain intact during the real Gasp clip.");
				Check(conePart.IsInsideTree(), "The real Gasp scene tree must still contain the Cone part.");
				zombie.AnimeCompleted("Gasp");
				await WaitFrames(2);
				string atlasReplacePath = zombie.sprite.GetAtlasReplacePath("Zombie_head.png");
				AdobeAnimateExternalTextureAtlasAllocation allocation;
				bool flag = AdobeAnimateGlobalAtlasCache.TryGetReplaceTextureAllocation(atlasReplacePath, out allocation);
				Check(zombie.angry && Math.Abs(zombie.timeScaleInit - 3.0) < 0.0001 && atlasReplacePath == "uid://s4hnj2igecma" && flag && allocation.UsesTextureArray, "Gasp completion must enable anger and install the authored angry head texture.");
				Check(zombie.CurrentStateHandle?.StableId == "zombie.walk" && zombie.headSlot.followSlotId == 17 && zombie.sprite.clip == "AngryWalk", $"Anger must restore the authored head layer and enter AngryWalk; state={zombie.CurrentStateHandle?.StableId}, follow={zombie.headSlot.followSlotId}, clip={zombie.sprite.clip}.");
				int num = (int)zombie.sprite.flashAnimeData.mediaDictionary["Zombie_head.png"];
				AdobeAnimateCrowdRenderState state;
				AdobeAnimateCrowdRenderStateResult adobeAnimateCrowdRenderStateResult = zombie.sprite.TryBuildCrowdRenderState(out state);
				AdobeAnimateGpuGraphOwnerState adobeAnimateGpuGraphOwnerState = state?.GpuGraphRootOwnerState;
				Check(adobeAnimateCrowdRenderStateResult == AdobeAnimateCrowdRenderStateResult.Submitted && state != null && state.Mode == AdobeAnimateCrowdRenderMode.GpuGraph && adobeAnimateGpuGraphOwnerState != null && adobeAnimateGpuGraphOwnerState.HasMediaReplace && num >= 0 && num < adobeAnimateGpuGraphOwnerState.MediaReplaceUse.Count && adobeAnimateGpuGraphOwnerState.MediaReplaceUse[num] && adobeAnimateGpuGraphOwnerState.MediaReplaceRect[num] == allocation.Rect && adobeAnimateGpuGraphOwnerState.MediaReplaceAtlasPages[num] == allocation.AtlasPage, "愤怒头像必须进入读报僵尸下一次 GPU Graph 根角色提交，而不只是保留逻辑路径。");
				Check(zombie.instance.ArmorHas("Cone") && cone.sprite == conePart && conePart.externalAtlasTexturePath == coneTexturePath && conePart.GetParent() == zombie.headSlot, "AngryWalk must preserve the exact Cone part, atlas path, and HeadSlot binding.");
				Check(conePart.IsInsideTree() && AdobeAnimateManagedSprite2D.GetLogicalVisible(conePart), "AngryWalk must keep the Cone shared-atlas part logically visible.");
				Check(conePart.externalAtlasCentered, "AngryWalk must preserve the Cone part centering.");
				zombie.AttackEntered();
				await WaitFrames(2);
				Check(zombie.sprite.clip == "AngryEat", "The angry attack callback must enter AngryEat; clip=" + zombie.sprite.clip + ".");
				Check(zombie.instance.ArmorHas("Cone") && cone.sprite == conePart && conePart.externalAtlasTexturePath == coneTexturePath, "AngryEat must retain the same packet-authored Cone atlas part.");
				Check(conePart.IsInsideTree() && AdobeAnimateManagedSprite2D.GetLogicalVisible(conePart), "AngryEat must keep the Cone part logically visible.");
				zombie.sprite.SetAtlasReplace("Zombie_head.png", string.Empty);
				zombie.ImportVariantSave(new Dictionary { { "angry", true } });
				await WaitFrames(2);
				Check(zombie.sprite.GetAtlasReplacePath("Zombie_head.png") == "uid://s4hnj2igecma" && zombie.walkAnimeClip == "AngryWalk" && zombie.attackAnimeClip == "AngryEat", "恢复愤怒存档时必须重新应用头像、行走和攻击表现。");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[{"BugOverviewNewspaperArmorAngryVisualRuntimeTest"}] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(zombie) && !zombie.IsQueuedForDeletion())
			{
				zombie.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			await WaitFrames(4);
		}
		bool flag2 = _checks == 26 && _failures == 0;
		GD.Print($"{"BUG_OVERVIEW_NEWSPAPER_ARMOR_ANGRY_VISUAL_RESULT"} passed={flag2} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag2) ? 2 : 0);
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
			_failures++;
			GD.PushError("[BugOverviewNewspaperArmorAngryVisualRuntimeTest] " + message);
		}
	}
}
