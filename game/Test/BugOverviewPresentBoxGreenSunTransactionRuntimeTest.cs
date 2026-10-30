using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewPresentBoxGreenSunTransactionRuntimeTest : Node
{
	private const string PresentBoxPacketPath = "res://Asset/Anime/Character/Plant/Special/PresentBoxGreen/Packet/PlantPresentBoxGreen.tres";

	private const string PresentBoxScenePath = "res://Asset/Anime/Character/Plant/Special/PresentBoxGreen/Scene/TowerDefensePlantPresentBoxGreen.tscn";

	private const string PeaShooterPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Packet/PlantPeaShooter.tres";

	private const string PeaShooterScenePath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Scene/TowerDefensePlantPeaShooter.tscn";

	private static readonly Vector2I InsufficientGrid = new Vector2I(2, 2);

	private static readonly Vector2I AffordableGrid = new Vector2I(3, 2);

	private int _checks;

	private int _failures;

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private TowerDefensePacketBankData _previousPresentBoxBank;

	private bool _presentBoxBankWasMissing;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		PresentBoxGreenSunTransactionRuntimeControlStub control = null;
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
				RegisterFixtures();
				control = new PresentBoxGreenSunTransactionRuntimeControlStub
				{
					Name = "PresentBoxGreenSunTransactionRuntimeControl",
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
				TowerDefenseBattleFeatureSun sunFeature = new TowerDefenseBattleFeatureSun
				{
					control = control
				};
				sunFeature.Init(new Dictionary { ["Begin"] = 250L });
				control.featureDictionary[new StringName("Sun")] = sunFeature;
				TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantPeaShooter");
				Check(GodotObject.IsInstanceValid(packetConfig) && packetConfig.characterConfig.cost == 200, "The fixture must use the real 200-sun PeaShooter replacement packet.");
				await VerifyInsufficientSunStillReplacesAndCharges(manager, sunFeature);
				await VerifyAffordableReplacementIsChargedOnce(manager, sunFeature);
				await VerifyNegativeBalanceDisablesSeedSlot(manager, sunFeature, control);
				goto end_IL_006f;
				end_IL_0086:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewPresentBoxGreenSunTransactionRuntimeTest] Unexpected exception: {value}");
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
			RestoreFixtures();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag = _failures == 0 && _checks == 27;
		GD.Print($"PRESENT_BOX_GREEN_SUN_TRANSACTION_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyInsufficientSunStillReplacesAndCharges(TowerDefenseManager manager, TowerDefenseBattleFeatureSun sunFeature)
	{
		Check(manager.SetSun(EconomyAccountId.Local, 50L), "The insufficient-sun scenario must set the real local ledger to 50.");
		TowerDefensePlantPresentBoxGreen presentBox = SpawnPresentBox(InsufficientGrid);
		Check(GodotObject.IsInstanceValid(presentBox), "The real green PresentBox must spawn for the insufficient-sun scenario.");
		await WaitFrames(5);
		TowerDefenseCellInstance cell = TowerDefenseManager.GetMapCell(InsufficientGrid);
		Check(cell.HasCharacter("PlantPresentBoxGreen"), "The real green PresentBox must occupy its map cell before opening.");
		presentBox.Explode();
		await WaitFrames(5);
		Check(sunFeature.GetSun(EconomyAccountId.Local) == -150, "A 200-sun replacement must be charged from 50 sun, leaving a -150 balance.");
		Check(!cell.HasCharacter("PlantPresentBoxGreen"), "Insufficient sun must not restore the green PresentBox after it opens.");
		Check(cell.HasCharacter("PlantPeaShooter"), "Insufficient sun must still create the selected replacement plant.");
	}

	private async Task VerifyAffordableReplacementIsChargedOnce(TowerDefenseManager manager, TowerDefenseBattleFeatureSun sunFeature)
	{
		Check(manager.SetSun(EconomyAccountId.Local, 250L), "The affordable scenario must set the real local ledger to 250.");
		TowerDefensePlantPresentBoxGreen presentBox = SpawnPresentBox(AffordableGrid);
		Check(GodotObject.IsInstanceValid(presentBox), "The real green PresentBox must spawn for the affordable scenario.");
		await WaitFrames(5);
		TowerDefenseCellInstance cell = TowerDefenseManager.GetMapCell(AffordableGrid);
		Check(cell.HasCharacter("PlantPresentBoxGreen"), "The affordable green PresentBox must occupy its map cell before opening.");
		presentBox.Explode();
		await WaitFrames(5);
		Check(sunFeature.GetSun(EconomyAccountId.Local) == 50, "A successful 200-sun replacement must charge exactly once from 250.");
		Check(!cell.HasCharacter("PlantPresentBoxGreen"), "A successful replacement must remove the green PresentBox from its cell.");
		Check(cell.HasCharacter("PlantPeaShooter"), "Sufficient sun must create the selected real PeaShooter replacement.");
	}

	private async Task VerifyNegativeBalanceDisablesSeedSlot(TowerDefenseManager manager, TowerDefenseBattleFeatureSun sunFeature, PresentBoxGreenSunTransactionRuntimeControlStub control)
	{
		TowerDefensePacketConfig presentBoxPacket = TowerDefenseManager.GetPacketConfig("PlantPresentBoxGreen");
		Check(GodotObject.IsInstanceValid(presentBoxPacket) && presentBoxPacket.disableWhenSunNegative && presentBoxPacket.GetCost() == 0, "The real zero-cost self-pay PresentBox card must declare its negative-Sun boundary.");
		TowerDefenseInGamePacketShow slot = TowerDefenseManager.CreatePacketShow();
		Check(GodotObject.IsInstanceValid(slot), "The self-pay PresentBox regression must create a real in-game card.");
		if (GodotObject.IsInstanceValid(slot))
		{
			control.characterNode.AddChild(slot, forceReadableName: false, InternalMode.Disabled);
			await WaitFrames(2);
			slot.Init(presentBoxPacket);
			slot.useCost = true;
			slot.start = true;
			slot.coldDownOpen = false;
			Check(slot.TryBindSunAccount(EconomyAccountId.Local), "The self-pay PresentBox card must bind the local Sun account.");
			Check(slot.itemCost == 0, $"The regression must cover the zero-cost affordability edge; cost={slot.itemCost}.");
			int pressedCount = 0;
			slot.OnPressed += delegate
			{
				pressedCount++;
			};
			Check(manager.SetSun(EconomyAccountId.Local, -1L), "The negative-balance card scenario must set the local ledger to -1.");
			slot.alive = true;
			slot.Pressed();
			Check(pressedCount == 0 && !slot.select, "A self-pay PresentBox click must be ignored immediately after Sun becomes negative.");
			Check(!slot.alive, "The rejected stale click must synchronously mark the self-pay PresentBox unavailable.");
			Check(slot.layout?.Modulate == Colors.DimGray, "The negative-balance self-pay PresentBox must render dimmed.");
			slot.alive = true;
			SunSpendReceipt spendReceipt;
			bool flag = slot.TryBeginPendingUse(out spendReceipt);
			Check(!flag && spendReceipt == null, "A previously selected self-pay PresentBox cannot begin placement while Sun is negative.");
			Check(sunFeature.GetSun(EconomyAccountId.Local) == -1, "Rejecting the self-pay PresentBox must not mutate the negative balance.");
			Check(manager.SetSun(EconomyAccountId.Local, 0L), "The recovery scenario must restore the local ledger to zero.");
			await WaitFrames(3);
			Check(slot.alive && slot.layout?.Modulate == Colors.White, "The self-pay PresentBox must become bright and usable again at zero Sun.");
		}
	}

	private static TowerDefensePlantPresentBoxGreen SpawnPresentBox(Vector2I gridPos)
	{
		return TowerDefenseManager.GetPacketConfig("PlantPresentBoxGreen")?.Plant(gridPos, playAudio: false) as TowerDefensePlantPresentBoxGreen;
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

	private void RegisterFixtures()
	{
		RegisterPacket("PlantPresentBoxGreen", "res://Asset/Anime/Character/Plant/Special/PresentBoxGreen/Packet/PlantPresentBoxGreen.tres");
		RegisterPacket("PlantPeaShooter", "res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Packet/PlantPeaShooter.tres");
		RegisterCharacter("PlantPresentBoxGreen", "res://Asset/Anime/Character/Plant/Special/PresentBoxGreen/Scene/TowerDefensePlantPresentBoxGreen.tscn");
		RegisterCharacter("PlantPeaShooter", "res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Scene/TowerDefensePlantPeaShooter.tscn");
		ResourceManager instance = ResourceManager.Instance;
		if (instance.TOWERDEFENSE_PACKETBANKS.TryGetValue("PlantPresentBox", out var value))
		{
			_previousPresentBoxBank = value;
		}
		else
		{
			_presentBoxBankWasMissing = true;
		}
		instance.TOWERDEFENSE_PACKETBANKS["PlantPresentBox"] = new TowerDefensePacketBankData
		{
			category = new Dictionary { ["White"] = new Array<string> { "PlantPeaShooter" } }
		};
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

	private void RestoreFixtures()
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
		if (_presentBoxBankWasMissing)
		{
			instance.TOWERDEFENSE_PACKETBANKS.Remove("PlantPresentBox");
		}
		else
		{
			instance.TOWERDEFENSE_PACKETBANKS["PlantPresentBox"] = _previousPresentBoxBank;
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
			GD.PushError("[BugOverviewPresentBoxGreenSunTransactionRuntimeTest] " + message);
		}
	}
}
