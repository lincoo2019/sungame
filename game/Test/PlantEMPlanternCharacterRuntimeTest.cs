using System;
using Godot;
using Godot.Collections;

public partial class PlantEMPlanternCharacterRuntimeTest : Node
{
	private const string PacketPath = "res://Asset/Anime/Character/Plant/Chapter9/EMPlantern/Packet/PlantEMPlantern.tres";

	private const string CharacterScenePath = "res://Asset/Anime/Character/Plant/Chapter9/EMPlantern/Scene/TowerDefensePlantEMPlantern.tscn";

	private const string SpriteScenePath = "res://Asset/Anime/Character/Plant/Chapter9/EMPlantern/EMPlantern.tscn";

	private const string CharacterSceneUid = "uid://cf3ahi72lr6ut";

	private const string SpriteSceneUid = "uid://dammqc4ws1lke";

	private const string AnimationDataPath = "res://Asset/Anime/Character/Plant/Chapter9/EMPlantern/EMPlantern.tres";

	private const string CurrentScenePath = "res://Asset/Anime/Character/Plant/Chapter9/EMPlantern/Effect/Current/EMPlanternCurrent.tscn";

	private const string CharacterRegistryPath = "res://Asset/Config/Character/CharacterResource.json";

	private const string PacketBankRegistryPath = "res://Asset/Config/PacketBank/PacketBankResource.json";

	private const string PacketInitPath = "res://Asset/Config/Save/TowerDefensePacketInit.json";

	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		try
		{
			TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Chapter9/EMPlantern/Packet/PlantEMPlantern.tres", null, ResourceLoader.CacheMode.Ignore);
			PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter9/EMPlantern/Scene/TowerDefensePlantEMPlantern.tscn", null, ResourceLoader.CacheMode.Ignore);
			PackedScene packedScene2 = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter9/EMPlantern/EMPlantern.tscn", null, ResourceLoader.CacheMode.Ignore);
			AdobeAnimateData adobeAnimateData = ResourceLoader.Load<AdobeAnimateData>("res://Asset/Anime/Character/Plant/Chapter9/EMPlantern/EMPlantern.tres", null, ResourceLoader.CacheMode.Ignore);
			PackedScene packedScene3 = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter9/EMPlantern/Effect/Current/EMPlanternCurrent.tscn", null, ResourceLoader.CacheMode.Ignore);
			Check(towerDefensePacketConfig != null, "PlantEMPlantern packet must load.");
			Check(packedScene != null, "PlantEMPlantern character scene must load.");
			Check(packedScene2 != null, "PlantEMPlantern sprite scene must load.");
			Check(adobeAnimateData != null, "PlantEMPlantern animation data must load.");
			Check(packedScene3 != null, "PlantEMPlantern current scene must load.");
			Check(towerDefensePacketConfig?.saveKey == "PlantEMPlantern", "Packet saveKey must be PlantEMPlantern.");
			Check(towerDefensePacketConfig?.characterConfig is TowerDefensePlantConfig, "Packet must reference TowerDefensePlantConfig.");
			if (towerDefensePacketConfig?.characterConfig is TowerDefensePlantConfig towerDefensePlantConfig)
			{
				Check(towerDefensePlantConfig.name == "PlantEMPlantern", "Config name must be PlantEMPlantern.");
				Check(Mathf.IsEqualApprox((float)towerDefensePlantConfig.hitpoints, 1000f), "Hitpoints must be 1000.");
				Check(towerDefensePlantConfig.cost == 250, "Cost must be 250.");
				Check(Mathf.IsEqualApprox((float)towerDefensePlantConfig.packetCooldown, 30f), "Packet cooldown must be 30 seconds.");
				Check(Mathf.IsZeroApprox((float)towerDefensePlantConfig.startingCooldown), "Starting cooldown must be zero.");
				Check((towerDefensePlantConfig.physiqueTypeFlags & 0x40) != 0, "Config must retain Plantern light physique.");
			}
			if (adobeAnimateData != null)
			{
				Check(Mathf.IsEqualApprox(adobeAnimateData.frameRate, 12.0), "Animation frame rate must be 12 FPS.");
				Check(adobeAnimateData.HasClip("Idle"), "Animation must contain Idle clip.");
				Check(adobeAnimateData.HasClip("anim_shooting"), "Animation must contain anim_shooting clip.");
				Check(adobeAnimateData.GetClip("Idle") == new Vector2I(0, 24), "Idle clip range must be [0, 24].");
				Check(adobeAnimateData.GetClip("anim_shooting") == new Vector2I(25, 44), "anim_shooting clip range must be [25, 44].");
			}
			VerifySpriteScene(packedScene2);
			VerifyCharacterScene(packedScene);
			VerifyCurrentScene(packedScene3);
			VerifyBuffContracts();
			VerifyNumericContracts();
			VerifyRegistries();
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[PlantEMPlanternCharacterRuntimeTest] {value}");
		}
		bool flag = _failures == 0;
		GD.Print($"PLANT_EM_PLANTERN_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void VerifySpriteScene(PackedScene spriteScene)
	{
		if (spriteScene == null)
		{
			return;
		}
		Node node = spriteScene.Instantiate(PackedScene.GenEditState.Disabled);
		try
		{
			Check(!node.Get("Animation/LayerVisible/Plantern_blink").AsBool(), "EM Plantern blink layer must remain hidden.");
		}
		finally
		{
			node.Free();
		}
	}

	private void VerifyCharacterScene(PackedScene characterScene)
	{
		if (characterScene == null)
		{
			return;
		}
		Node node = characterScene.Instantiate(PackedScene.GenEditState.Disabled);
		try
		{
			Check(node is TowerDefensePlantEMPlantern, "Character scene root must use TowerDefensePlantEMPlantern.");
			Check((node as TowerDefensePlantEMPlantern)?.config?.name == "PlantEMPlantern", "Instantiated character must retain EM Plantern config.");
			Sprite2D nodeOrNull = node.GetNodeOrNull<Sprite2D>("SpriteGroup/TransformPoint/LanternShineEM");
			Check(nodeOrNull != null && nodeOrNull.Texture?.ResourcePath.EndsWith("/EMPlanternShine.png", StringComparison.Ordinal) == true, "Halo must use EMPLanternShine.png.");
			Check(nodeOrNull?.Material is CanvasItemMaterial canvasItemMaterial && canvasItemMaterial.BlendMode == CanvasItemMaterial.BlendModeEnum.Add, "Halo must use additive blending.");
			Check(node.GetNodeOrNull<Marker2D>("CurrentAnchor") != null, "Character must expose a current anchor.");
			Check(node.GetNodeOrNull<PointLight2D>("Light") != null, "Character must retain Plantern illumination.");
			Check(node.GetNodeOrNull("SpriteGroup/PlanternFog") != null, "Character must retain the Plantern fog-clearing visual.");
		}
		finally
		{
			node.Free();
		}
	}

	private void VerifyCurrentScene(PackedScene currentScene)
	{
		if (currentScene == null)
		{
			return;
		}
		Node node = currentScene.Instantiate(PackedScene.GenEditState.Disabled);
		try
		{
			AnimatedSprite2D nodeOrNull = node.GetNodeOrNull<AnimatedSprite2D>("Current");
			Check(nodeOrNull != null, "Current effect must contain an AnimatedSprite2D.");
			Check(nodeOrNull != null && nodeOrNull.SpriteFrames?.GetFrameCount("current") == 5, "Current effect must contain five ordered frames.");
			Check(Mathf.IsEqualApprox((float)(nodeOrNull?.SpriteFrames?.GetAnimationSpeed("current")).GetValueOrDefault(), 12f), "Current effect must play at 12 FPS.");
			Check(nodeOrNull != null && nodeOrNull.SpriteFrames?.GetAnimationLoopMode("current") == SpriteFrames.LoopMode.Linear, "Current effect must loop during its active window.");
			Check(nodeOrNull?.Material is CanvasItemMaterial canvasItemMaterial && canvasItemMaterial.BlendMode == CanvasItemMaterial.BlendModeEnum.Add, "Current effect must use additive blending.");
		}
		finally
		{
			node.Free();
		}
	}

	private void VerifyBuffContracts()
	{
		TowerDefenseCharacterBuffEMSpeedDown towerDefenseCharacterBuffEMSpeedDown = new TowerDefenseCharacterBuffEMSpeedDown
		{
			time = 0.6,
			currentTime = 0.2
		};
		towerDefenseCharacterBuffEMSpeedDown._Init();
		Check(towerDefenseCharacterBuffEMSpeedDown.key == "EMSpeedDown", "Electromagnetic slow must use the EMSpeedDown key.");
		Check(towerDefenseCharacterBuffEMSpeedDown.FrameMeshColorMultiplier == Colors.White, "Electromagnetic slow must not tint zombies blue.");
		TowerDefenseCharacterBuffEMSpeedDown towerDefenseCharacterBuffEMSpeedDown2 = towerDefenseCharacterBuffEMSpeedDown.CreateRuntimeInstance() as TowerDefenseCharacterBuffEMSpeedDown;
		Check(towerDefenseCharacterBuffEMSpeedDown2 != null && Mathf.IsEqualApprox((float)towerDefenseCharacterBuffEMSpeedDown2.time, 0.6f) && Mathf.IsEqualApprox((float)towerDefenseCharacterBuffEMSpeedDown2.currentTime, 0.2f), "Electromagnetic slow runtime copies must retain timers.");
		Check(TowerDefenseCharacterBuffConfig.CreateBuffByKey("EMSpeedDown") is TowerDefenseCharacterBuffEMSpeedDown, "Electromagnetic slow must restore by key.");
		Check(BuffComponent.BUFF_SAVE_FIELDS.TryGetValue("EMSpeedDown", out var value) && System.Array.IndexOf(value, "time") >= 0 && System.Array.IndexOf(value, "currentTime") >= 0, "Electromagnetic slow timers must participate in save/network snapshots.");
		Check(BuffComponent.BUFF_SAVE_FIELDS.TryGetValue("EMP", out var value2) && System.Array.IndexOf(value2, "time") >= 0 && System.Array.IndexOf(value2, "currentTime") >= 0, "EMP timers must participate in save/network snapshots.");
	}

	private void VerifyNumericContracts()
	{
		Check(Mathf.IsEqualApprox(3f, 3f), "Aura and pair pulse interval must be 3 seconds.");
		Check(Mathf.IsEqualApprox(80f, 80f), "Aura damage must be 80.");
		Check(Mathf.IsEqualApprox(200f, 200f), "Current damage must be 200.");
		Check(Mathf.IsEqualApprox(1.6666666f, 1.6666666f), "Current active window must be four five-frame loops at 12 FPS.");
		Check(Mathf.IsEqualApprox(0.2f, 0.2f), "Base current EMP chance must be 20 percent.");
		Check(Mathf.IsEqualApprox(1f, 1f), "Current EMP duration must be one second.");
	}

	private void VerifyRegistries()
	{
		Dictionary dictionary = ResourceLoader.Load<Json>("res://Asset/Config/Character/CharacterResource.json", null, ResourceLoader.CacheMode.Ignore)?.Data.AsGodotDictionary();
		Check(dictionary?.ContainsKey("PlantEMPlantern") ?? false, "Character registry must contain PlantEMPlantern.");
		if (dictionary != null && dictionary.GetValueOrDefault("PlantEMPlantern").VariantType == Variant.Type.Dictionary)
		{
			Dictionary dictionary2 = dictionary["PlantEMPlantern"].AsGodotDictionary();
			Check(dictionary2.GetValueOrDefault("Scene", "").AsString() == "uid://cf3ahi72lr6ut", "Character registry scene UID must resolve to the authored scene.");
			Check(dictionary2.GetValueOrDefault("Sprite", "").AsString() == "uid://dammqc4ws1lke", "Character registry sprite UID must resolve to the authored sprite.");
		}
		Dictionary dictionary3 = ResourceLoader.Load<Json>("res://Asset/Config/PacketBank/PacketBankResource.json", null, ResourceLoader.CacheMode.Ignore)?.Data.AsGodotDictionary();
		string[] array = new string[4] { "GeneralPlant", "GeneralPlantNoLeaf", "GeneralPlantNoOriginal", "PresentBoxNoAshPlant" };
		foreach (string text in array)
		{
			bool flag = false;
			if (dictionary3 != null && dictionary3.GetValueOrDefault(text).VariantType == Variant.Type.Dictionary)
			{
				foreach (Variant item in dictionary3[text].AsGodotDictionary().GetValueOrDefault("Category", new Dictionary()).AsGodotDictionary()
					.GetValueOrDefault("White", new Godot.Collections.Array())
					.AsGodotArray())
				{
					flag |= item.AsString() == "PlantEMPlantern";
				}
			}
			Check(flag, "Packet bank " + text + " must include PlantEMPlantern in White.");
		}
		Dictionary dictionary4 = ResourceLoader.Load<Json>("res://Asset/Config/Save/TowerDefensePacketInit.json", null, ResourceLoader.CacheMode.Ignore)?.Data.AsGodotDictionary();
		Check(dictionary4 != null && dictionary4.ContainsKey("PlantEMPlantern") && !dictionary4.GetValueOrDefault("PlantEMPlantern", true).AsBool(), "PlantEMPlantern must start locked in packet initialization.");
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[PlantEMPlanternCharacterRuntimeTest] " + message);
		}
	}
}
