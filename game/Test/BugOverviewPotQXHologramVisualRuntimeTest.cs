using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewPotQXHologramVisualRuntimeTest : Node
{
	private const string PotQxPacketPath = "res://Asset/Anime/Character/Plant/Star/PotQX/Packet/PlantPotQX.tres";

	private const string PotQxScenePath = "res://Asset/Anime/Character/Plant/Star/PotQX/Scene/TowerDefensePlantPotQX.tscn";

	private const string SourcePacketPath = "res://Asset/Anime/Character/Plant/Chapter1/SunflowerPea/Packet/PlantSunflowerPea.tres";

	private const string SourceScenePath = "res://Asset/Anime/Character/Plant/Chapter1/SunflowerPea/Scene/TowerDefensePlantSunflowerPea.tscn";

	private static readonly Vector2I SourceGrid = new Vector2I(4, 2);

	private int _checks;

	private int _failures;

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		PotQxHologramVisualRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 2;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_0086;
				}
				RegisterRealFixtures();
				control = new PotQxHologramVisualRuntimeControlStub
				{
					Name = "PotQxHologramVisualRuntimeControl",
					isGameRunning = true,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig()
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				Node2D node2D = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = node2D;
				manager.currentControl = control;
				manager.gridBeginPos = Vector2.Zero;
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				TowerDefensePacketConfig towerDefensePacketConfig = LoadPacket("res://Asset/Anime/Character/Plant/Chapter1/SunflowerPea/Packet/PlantSunflowerPea.tres");
				TowerDefensePacketConfig potQxPacket = LoadPacket("res://Asset/Anime/Character/Plant/Star/PotQX/Packet/PlantPotQX.tres");
				TowerDefensePlant source = towerDefensePacketConfig?.Plant(SourceGrid, playAudio: false) as TowerDefensePlant;
				await WaitFrames(4);
				TowerDefensePlantPotQX potQx = potQxPacket?.Plant(SourceGrid, playAudio: false) as TowerDefensePlantPotQX;
				await WaitFrames(4);
				Check(GodotObject.IsInstanceValid(source) && GodotObject.IsInstanceValid(potQx), "Real SunflowerPea and PotQX scenes must share the projection-source cell.");
				if (!GodotObject.IsInstanceValid(source) || !GodotObject.IsInstanceValid(potQx))
				{
					goto end_IL_0086;
				}
				potQx.CreateHologramCharacter();
				await WaitFrames(8);
				Check(potQx.createCharacterList.Count == manager.gridNum.Y - 1, $"PotQX must create one projection on every other row; got {potQx.createCharacterList.Count}.");
				int num = ShaderEffectComponent.ShaderEffectFlags["hologram"];
				foreach (TowerDefenseCharacter createCharacter in potQx.createCharacterList)
				{
					Check(GodotObject.IsInstanceValid(createCharacter) && createCharacter.config?.name == "PlantSunflowerPea", "Every PotQX copy must be a real SunflowerPea scene.");
					Check(createCharacter.instance.hologram && (createCharacter.shaderEffectComponent.GetEffectFlags() & num) != 0, "Every PotQX copy must carry both gameplay and render hologram flags.");
					List<AdobeAnimateSpriteBase> list = CollectAdobeSprites(createCharacter.sprite);
					Check(list.Count >= 2, "The real SunflowerPea projection must expose its body and nested head sprites.");
					Check(HasHologramPresentation(createCharacter.spriteGroup), "Every body part of a PotQX projection must be cyan-tinted and translucent instead of a solid normal texture.");
				}
				Check((source.shaderEffectComponent.GetEffectFlags() & num) == 0, "The source plant must not inherit the projection hologram flag.");
				Check(IsNormalPresentation(source.spriteGroup), "The source plant must keep its normal opaque presentation.");
				goto end_IL_006f;
				end_IL_0086:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewPotQXHologramVisualRuntimeTest] Unexpected exception: {value}");
				goto end_IL_006f;
			}
			return;
			end_IL_006f:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
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
			RestoreRealFixtures();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag = _failures == 0;
		GD.Print($"POT_QX_HOLOGRAM_VISUAL_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static bool HasHologramPresentation(CanvasItem canvasItem)
	{
		Color modulate = canvasItem.Modulate;
		if (modulate.A <= 0.76f && modulate.B >= modulate.R + 0.2f)
		{
			return modulate.G >= modulate.R + 0.15f;
		}
		return false;
	}

	private static bool IsNormalPresentation(CanvasItem canvasItem)
	{
		Color modulate = canvasItem.Modulate;
		if (Mathf.IsEqualApprox(modulate.R, 1f) && Mathf.IsEqualApprox(modulate.G, 1f) && Mathf.IsEqualApprox(modulate.B, 1f))
		{
			return Mathf.IsEqualApprox(modulate.A, 1f);
		}
		return false;
	}

	private static List<AdobeAnimateSpriteBase> CollectAdobeSprites(Node root)
	{
		List<AdobeAnimateSpriteBase> list = new List<AdobeAnimateSpriteBase>();
		if (!GodotObject.IsInstanceValid(root))
		{
			return list;
		}
		Stack<Node> stack = new Stack<Node>();
		stack.Push(root);
		while (stack.Count > 0)
		{
			Node node = stack.Pop();
			if (node is AdobeAnimateSpriteBase item)
			{
				list.Add(item);
			}
			for (int i = 0; i < node.GetChildCount(); i++)
			{
				stack.Push(node.GetChild(i));
			}
		}
		return list;
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig()
		});
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
		return towerDefenseBattleFeatureMap;
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantPotQX", "res://Asset/Anime/Character/Plant/Star/PotQX/Packet/PlantPotQX.tres");
		RegisterPacket("PlantSunflowerPea", "res://Asset/Anime/Character/Plant/Chapter1/SunflowerPea/Packet/PlantSunflowerPea.tres");
		RegisterCharacter("PlantPotQX", "res://Asset/Anime/Character/Plant/Star/PotQX/Scene/TowerDefensePlantPotQX.tscn");
		RegisterCharacter("PlantSunflowerPea", "res://Asset/Anime/Character/Plant/Chapter1/SunflowerPea/Scene/TowerDefensePlantSunflowerPea.tscn");
	}

	private void RegisterPacket(string key, string path)
	{
		ResourceManager instance = ResourceManager.Instance;
		if (instance.TOWERDEFENSE_PACKETS.TryGetValue(key, out var value))
		{
			_previousPackets[key] = value;
		}
		else
		{
			_missingPackets.Add(key);
		}
		instance.TOWERDEFENSE_PACKETS[key] = ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore);
	}

	private void RegisterCharacter(string key, string path)
	{
		ResourceManager instance = ResourceManager.Instance;
		if (instance.TOWERDEFENSE_CHARCATERS.TryGetValue(key, out var value))
		{
			_previousCharacters[key] = value;
		}
		else
		{
			_missingCharacters.Add(key);
		}
		instance.TOWERDEFENSE_CHARCATERS[key] = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
	}

	private void RestoreRealFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return;
		}
		foreach (string missingPacket in _missingPackets)
		{
			instance.TOWERDEFENSE_PACKETS.Remove(missingPacket);
		}
		foreach (KeyValuePair<string, Resource> previousPacket in _previousPackets)
		{
			instance.TOWERDEFENSE_PACKETS[previousPacket.Key] = previousPacket.Value;
		}
		foreach (string missingCharacter in _missingCharacters)
		{
			instance.TOWERDEFENSE_CHARCATERS.Remove(missingCharacter);
		}
		foreach (KeyValuePair<string, Resource> previousCharacter in _previousCharacters)
		{
			instance.TOWERDEFENSE_CHARCATERS[previousCharacter.Key] = previousCharacter.Value;
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
			GD.PushError("[BugOverviewPotQXHologramVisualRuntimeTest] " + message);
		}
	}
}
