using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewMobileRoofFifthRowTapRuntimeTest : Node
{
	[Export(PropertyHint.None, "")]
	public bool repeatSelectionOnly;

	[Export(PropertyHint.None, "")]
	public bool droppedCardSelectionOnly;

	private const string RoofConfigPath = "res://Asset/Config/Map/Roof/Config/FrontlawnMapRoof.tres";

	private const string PotPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Pot/Packet/PlantPot.tres";

	private const string PotScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Pot/Scene/TowerDefensePlantPot.tscn";

	private const string PotSpritePath = "res://Asset/Anime/Character/Plant/Chapter0/Pot/Pot.tscn";

	private int _checks;

	private int _failures;

	private Resource _previousPotCharacter;

	private bool _potCharacterWasMissing;

	private Resource _previousPotSprite;

	private bool _potSpriteWasMissing;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		bool previousMobile = Global.Instance?.isMobile ?? false;
		bool previousEditor = Global.Instance?.isEditor ?? false;
		bool previousMultiplayer = Global.Instance?.isMultiplayerMode ?? false;
		MobileRoofFifthRowTapRuntimeControlStub control = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		PacketPickControl packetPickControl = null;
		TowerDefenseMapConfig roofConfig = null;
		List<TowerDefenseInGamePacketShow> slots = new List<TowerDefenseInGamePacketShow>();
		try
		{
			_ = 5;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(Global.Instance) && GodotObject.IsInstanceValid(ResourceManager.Instance), "Manager, Global, and ResourceManager autoloads must be available.");
				Check(InputMap.HasAction("Press"), "The production Press action must exist for the mobile input regression.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(Global.Instance) || !GodotObject.IsInstanceValid(ResourceManager.Instance) || !InputMap.HasAction("Press"))
				{
					goto end_IL_0150;
				}
				RegisterPotCharacter();
				roofConfig = ResourceLoader.Load<TowerDefenseMapConfig>("res://Asset/Config/Map/Roof/Config/FrontlawnMapRoof.tres", null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefenseMapConfig;
				Check(GodotObject.IsInstanceValid(roofConfig) && roofConfig.gridNum == new Vector2I(9, 5), "The regression must use the real five-row Roof map config.");
				if (!GodotObject.IsInstanceValid(roofConfig))
				{
					goto end_IL_0150;
				}
				Global.Instance.isMobile = true;
				Global.Instance.isEditor = false;
				Global.Instance.isMultiplayerMode = false;
				control = new MobileRoofFifthRowTapRuntimeControlStub
				{
					Name = "MobileRoofFifthRowTapRuntimeControl",
					isGameRunning = false,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig
					{
						packetColdDownUse = repeatSelectionOnly
					}
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				MobileRoofFifthRowTapRuntimeMapControlStub mobileRoofFifthRowTapRuntimeMapControlStub = new MobileRoofFifthRowTapRuntimeMapControlStub
				{
					Name = "MapControl"
				};
				AddChild(mobileRoofFifthRowTapRuntimeMapControlStub, forceReadableName: false, InternalMode.Disabled);
				mobileRoofFifthRowTapRuntimeMapControlStub.spriteNode = new Node2D
				{
					Name = "SpriteNode"
				};
				mobileRoofFifthRowTapRuntimeMapControlStub.AddChild(mobileRoofFifthRowTapRuntimeMapControlStub.spriteNode, forceReadableName: false, InternalMode.Disabled);
				mapFeature = (mobileRoofFifthRowTapRuntimeMapControlStub.mapFeature = CreateRoofMapFeature(control, mobileRoofFifthRowTapRuntimeMapControlStub, roofConfig));
				control.featureDictionary[new StringName("Map")] = mapFeature;
				manager.gridBeginPos = roofConfig.gridBeginPos;
				manager.gridSize = roofConfig.gridSize;
				manager.gridNum = roofConfig.gridNum;
				packetPickControl = new PacketPickControl
				{
					Name = "PacketPickControl"
				};
				mobileRoofFifthRowTapRuntimeMapControlStub.AddChild(packetPickControl, forceReadableName: false, InternalMode.Disabled);
				packetPickControl.Init(mobileRoofFifthRowTapRuntimeMapControlStub, mapFeature, new TowerDefenseBattleFeaturePacketPickConfig
				{
					packetSelectionDebounceFrames = 5
				});
				mapFeature.packetPickControl = packetPickControl;
				for (int index = 0; index < 4; index++)
				{
					slots.Add(await CreatePotSlot(control.characterNode, packetPickControl));
				}
				bool flag = slots.Count == 4;
				foreach (TowerDefenseInGamePacketShow item in slots)
				{
					flag &= GodotObject.IsInstanceValid(item);
				}
				Check(flag, "Four real Flower Pot cards must be ready for independent input routes.");
				if (!flag)
				{
					goto end_IL_0150;
				}
				VerifyRepeatSelectionChain(packetPickControl, slots);
				MethodInfo placementSurfaceMethod;
				if (droppedCardSelectionOnly)
				{
					await VerifyDroppedCardSelectionGesture(packetPickControl, mapFeature, slots[0]);
				}
				else if (repeatSelectionOnly)
				{
					await VerifyPlantCooldownRepeatSelection(packetPickControl, slots[0]);
				}
				else
				{
					Vector2I tapFifthGrid = new Vector2I(1, 5);
					Vector2I dragFifthGrid = new Vector2I(2, 5);
					Vector2I otherRowGrid = new Vector2I(3, 2);
					Vector2 placementPoint = GetPlacementPoint(mapFeature, tapFifthGrid);
					Vector2 dragFifthPoint = GetPlacementPoint(mapFeature, dragFifthGrid);
					Vector2 otherRowPoint = GetPlacementPoint(mapFeature, otherRowGrid);
					Check(!mapFeature.groundRect.HasPoint(placementPoint) && manager.GetMapGridPosFromMouse(placementPoint) == tapFifthGrid && GodotObject.IsInstanceValid(TowerDefenseManager.GetMapCell(tapFifthGrid)), $"The real sloped Roof row five point must be outside the legacy rectangle but resolve to a valid cell; point={placementPoint}, rect={mapFeature.groundRect}.");
					bool flag2 = true;
					for (int i = 1; i <= 4; i++)
					{
						Vector2I vector2I = new Vector2I(1, i);
						flag2 &= manager.GetMapGridPosFromMouse(GetPlacementPoint(mapFeature, vector2I)) == vector2I;
					}
					Check(flag2, "Roof rows one through four must retain their height-curve-aware coordinate mapping.");
					placementSurfaceMethod = typeof(PacketPickControl).GetMethod("IsPlacementSurfacePoint", BindingFlags.Instance | BindingFlags.NonPublic);
					Check(placementSurfaceMethod != null, "The mobile touch-down path must expose its placement-surface predicate.");
					if (placementSurfaceMethod == null)
					{
						goto end_IL_0150;
					}
					Check(IsPlacementSurface(placementPoint) && packetPickControl.packetPick == slots[0], "Touch-down on the valid fifth Roof row must be treated as placement surface and retain the card.");
					TowerDefenseCharacter tapPot = slots[0].Plant(tapFifthGrid, useSun: false);
					await WaitFrames(5);
					Check(tapPot is TowerDefensePlantPot && FindPot(control, tapFifthGrid) != null, "Point-select then tap must be able to place a real Flower Pot on Roof row five.");
					packetPickControl.Release();
					Check(!GodotObject.IsInstanceValid(packetPickControl.packetPick), "A successful point-selected placement must release the picked card.");
					SelectCard(slots[1]);
					Check(packetPickControl.packetPick == slots[1] && IsPlacementSurface(dragFifthPoint), "The existing drag target must remain a valid Roof row-five placement surface.");
					TowerDefenseCharacter dragPot = slots[1].Plant(dragFifthGrid, useSun: false);
					await WaitFrames(5);
					Check(dragPot is TowerDefensePlantPot && FindPot(control, dragFifthGrid) != null, "The existing drag-and-release route must still be able to place on Roof row five.");
					packetPickControl.Release();
					Check(!GodotObject.IsInstanceValid(packetPickControl.packetPick), "A successful dragged placement must release the picked card.");
					SelectCard(slots[2]);
					Check(packetPickControl.packetPick == slots[2] && mapFeature.groundRect.HasPoint(otherRowPoint) && IsPlacementSurface(otherRowPoint), "An ordinary Roof row touch must retain the selected card through touch-down.");
					TowerDefenseCharacter otherPot = slots[2].Plant(otherRowGrid, useSun: false);
					await WaitFrames(5);
					Check(otherPot is TowerDefensePlantPot && FindPot(control, otherRowGrid) != null, "Point-select then tap must continue to place on another Roof row.");
					packetPickControl.Release();
					SelectCard(slots[3]);
					Vector2 vector = roofConfig.gridBeginPos - new Vector2(120f, 120f);
					Check(packetPickControl.packetPick == slots[3] && !IsPlacementSurface(vector) && manager.GetMapGridPosFromMouse(vector).Y == -1, "A true off-map mobile touch must remain outside the placement surface.");
					if (!IsPlacementSurface(vector))
					{
						packetPickControl.Release();
					}
					Check(!GodotObject.IsInstanceValid(packetPickControl.packetPick), "The production off-map branch must still cancel the selected card.");
					Check(CountPlacedPots(control) == 3, "Exactly the fifth-row tap, fifth-row drag, and ordinary-row tap must place.");
				}
				goto end_IL_012d;
				end_IL_0150:
				bool IsPlacementSurface(Vector2 point)
				{
					return (bool)placementSurfaceMethod.Invoke(packetPickControl, new object[1] { point });
				}
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewMobileRoofFifthRowTapRuntimeTest] Unexpected exception: {value}");
				goto end_IL_012d;
			}
			return;
			end_IL_012d:;
		}
		finally
		{
			DispatchPressAction(pressed: false);
			if (GodotObject.IsInstanceValid(packetPickControl))
			{
				packetPickControl.DisposeBattleState();
			}
			if (GodotObject.IsInstanceValid(control?.characterNode))
			{
				foreach (Node child in control.characterNode.GetChildren())
				{
					if (!child.IsQueuedForDeletion())
					{
						child.QueueFree();
					}
				}
			}
			await WaitFrames(6);
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(Global.Instance))
			{
				Global.Instance.isMobile = previousMobile;
				Global.Instance.isEditor = previousEditor;
				Global.Instance.isMultiplayerMode = previousMultiplayer;
			}
			mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			RestorePotCharacter();
			await WaitFrames(6);
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			roofConfig?.Dispose();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		int num = (droppedCardSelectionOnly ? 14 : (repeatSelectionOnly ? 17 : 20));
		bool flag3 = _failures == 0 && _checks >= num;
		string value2 = (droppedCardSelectionOnly ? "DROPPED_CARD_SELECTION_GESTURE_RESULT" : (repeatSelectionOnly ? "PACKET_PICK_REPEAT_SELECTION_RESULT" : "MOBILE_ROOF_FIFTH_ROW_TAP_RESULT"));
		GD.Print($"{value2} passed={flag3} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag3) ? 2 : 0);
	}

	private async Task<TowerDefenseInGamePacketShow> CreatePotSlot(Node parent, PacketPickControl packetPickControl)
	{
		TowerDefensePacketConfig config = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Chapter0/Pot/Packet/PlantPot.tres", null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
		if (!GodotObject.IsInstanceValid(config))
		{
			return null;
		}
		TowerDefenseInGamePacketShow slot = TowerDefenseManager.CreatePacketShow();
		parent.AddChild(slot, forceReadableName: false, InternalMode.Disabled);
		await WaitFrames(2);
		slot.Init(config);
		slot.useCost = false;
		slot.start = true;
		slot.SetCentralRuntimeStateRefresh(enabled: true);
		slot.coldDownOpen = false;
		slot.alive = true;
		slot.OnPressed += packetPickControl.PickPacket;
		return slot;
	}

	private void VerifyRepeatSelectionChain(PacketPickControl packetPickControl, List<TowerDefenseInGamePacketShow> slots)
	{
		SelectCard(slots[0]);
		Check(packetPickControl.packetPick == slots[0], "The point-selection route must select the real Flower Pot card first.");
		packetPickControl.Release();
		Check(!GodotObject.IsInstanceValid(packetPickControl.packetPick) && !slots[0].select && slots[0].pressDelayTimer <= 0.0, "Putting a card back must clear its selection and the previous click debounce.");
		slots[0].Pressed();
		Check(packetPickControl.packetPick == slots[0] && slots[0].select, "The same card must be selectable again immediately after it is put back.");
		slots[1].Pressed();
		Check(packetPickControl.packetPick == slots[1] && slots[1].select && !slots[0].select && slots[0].pressDelayTimer <= 0.0, "Clicking another card must immediately release the previous card and select the new one.");
		slots[0].Pressed();
		Check(packetPickControl.packetPick == slots[0] && slots[0].select && !slots[1].select && slots[1].pressDelayTimer <= 0.0, "Rapidly switching back must not inherit the other card's selection or click debounce.");
	}

	private async Task VerifyPlantCooldownRepeatSelection(PacketPickControl packetPickControl, TowerDefenseInGamePacketShow slot)
	{
		TowerDefenseCharacter firstPot = slot.Plant(new Vector2I(1, 5), useSun: false);
		await WaitFrames(5);
		Check(firstPot is TowerDefensePlantPot && slot.coldDownOpen && !slot.select, "The first real placement must start cooldown and clear the card selection.");
		packetPickControl.Release();
		Check(!GodotObject.IsInstanceValid(packetPickControl.packetPick), "The first successful placement must release the picked card.");
		slot.coldDownTimer = 0.0;
		await WaitFrames(3);
		Check(!slot.coldDownOpen && slot.alive, "The real card must become alive again when its post-placement cooldown completes.");
		Check(!slot.coldDownProgressBar.Visible, "Cooldown completion must hide the overlay covering the real card button.");
		Check(slot.coldDownProgressBar.MouseFilter == Control.MouseFilterEnum.Ignore, "The visual cooldown overlay must never consume pointer input.");
		Check(slot.button.MouseFilter != Control.MouseFilterEnum.Ignore, "Cooldown completion must leave the real card button input enabled.");
		slot.button.EmitSignal(BaseButton.SignalName.Pressed);
		Check(packetPickControl.packetPick == slot && slot.select, "The real card Button signal must select the same card again after cooldown.");
		TowerDefenseCharacter secondPot = slot.Plant(new Vector2I(2, 5), useSun: false);
		await WaitFrames(5);
		Check(secondPot is TowerDefensePlantPot && slot.coldDownOpen, "The same real card must successfully plant a second time after cooldown.");
		packetPickControl.Release();
	}

	private async Task VerifyDroppedCardSelectionGesture(PacketPickControl packetPickControl, TowerDefenseBattleFeatureMap mapFeature, TowerDefenseInGamePacketShow slot)
	{
		Vector2I dropGrid = new Vector2I(4, 3);
		TowerDefenseCellInstance dropCell = TowerDefenseManager.GetMapCell(dropGrid);
		Vector2 dropPoint = GetPlacementPoint(mapFeature, dropGrid);
		packetPickControl.Release();
		slot.alive = true;
		slot.select = false;
		slot.pressDelayTimer = 0.0;
		DispatchPressAction(pressed: true);
		slot.button.EmitSignal(BaseButton.SignalName.Pressed);
		Check(packetPickControl.packetPick == slot && slot.select, "Pressing the real dropped-card button must select the card while the Press action is held.");
		for (int i = 0; i < 8; i++)
		{
			packetPickControl.ProcessPacketPick(dropCell, dropGrid, dropPoint);
		}
		Check(FindPot(TowerDefenseManager.Instance.currentControl, dropGrid) == null, "Holding a dropped card beyond the frame debounce must not plant it.");
		DispatchPressAction(pressed: false);
		bool flag = await WaitForPressRelease();
		packetPickControl.ProcessPacketPick(dropCell, dropGrid, dropPoint);
		Check(FindPot(TowerDefenseManager.Instance.currentControl, dropGrid) == null && packetPickControl.packetPick == slot && flag, "Releasing the gesture that selected a dropped card must be observed and retain the card instead of planting below it.");
		await WaitFrames(2);
		DispatchPressAction(pressed: true);
		packetPickControl.ProcessPacketPick(dropCell, dropGrid, dropPoint);
		Check(FindPot(TowerDefenseManager.Instance.currentControl, dropGrid) == null && packetPickControl.packetPick == slot, "A subsequent placement gesture must still wait for the mobile release confirmation.");
		await WaitFrames(2);
		DispatchPressAction(pressed: false);
		bool placementReleaseObserved = await WaitForPressRelease();
		packetPickControl.ProcessPacketPick(dropCell, dropGrid, dropPoint);
		await WaitFrames(5);
		TowerDefensePlantPot towerDefensePlantPot = FindPot(TowerDefenseManager.Instance.currentControl, dropGrid);
		Check(towerDefensePlantPot != null && !GodotObject.IsInstanceValid(packetPickControl.packetPick), $"A second independent tap must plant the selected dropped card and release the selection; placed={GodotObject.IsInstanceValid(towerDefensePlantPot)}, picked={GodotObject.IsInstanceValid(packetPickControl.packetPick)}, slot_alive={slot.alive}, slot_select={slot.select}, release_observed={placementReleaseObserved}.");
	}

	private async Task<bool> WaitForPressRelease()
	{
		for (int frame = 0; frame < 4; frame++)
		{
			if (Input.IsActionJustReleased("Press"))
			{
				return true;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (Input.IsActionJustReleased("Press"))
			{
				return true;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		return Input.IsActionJustReleased("Press");
	}

	private static TowerDefenseBattleFeatureMap CreateRoofMapFeature(TowerDefenseControlNew control, TowerDefenseMapControl mapControl, TowerDefenseMapConfig config)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = new TowerDefenseBattleFeatureMap();
		towerDefenseBattleFeatureMap.control = control;
		towerDefenseBattleFeatureMap.mapControl = mapControl;
		towerDefenseBattleFeatureMap.config = config;
		towerDefenseBattleFeatureMap.mapConfig = config;
		towerDefenseBattleFeatureMap.groundRect = new Rect2(config.gridBeginPos, config.gridSize * new Vector2(config.gridNum.X, config.gridNum.Y));
		towerDefenseBattleFeatureMap.PlantGridInit();
		return towerDefenseBattleFeatureMap;
	}

	private static Vector2 GetPlacementPoint(TowerDefenseBattleFeatureMap mapFeature, Vector2I gridPos)
	{
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
		return TowerDefenseManager.GetMapCellPlantPos(gridPos) - new Vector2(0f, (float)mapFeature.GetGroundHeight(mapCell));
	}

	private static void SelectCard(TowerDefenseInGamePacketShow slot)
	{
		slot.alive = true;
		slot.select = false;
		slot.pressDelayTimer = 0.0;
		slot.Pressed();
	}

	private static void DispatchPressAction(bool pressed)
	{
		Input.ParseInputEvent(new InputEventAction
		{
			Action = "Press",
			Pressed = pressed,
			Strength = (pressed ? 1f : 0f)
		});
		Input.FlushBufferedEvents();
	}

	private static TowerDefensePlantPot FindPot(TowerDefenseControlNew control, Vector2I gridPos)
	{
		foreach (Node child in control.characterNode.GetChildren())
		{
			if (child is TowerDefensePlantPot towerDefensePlantPot && towerDefensePlantPot.gridPos == gridPos)
			{
				return towerDefensePlantPot;
			}
		}
		return null;
	}

	private static int CountPlacedPots(TowerDefenseControlNew control)
	{
		int num = 0;
		foreach (Node child in control.characterNode.GetChildren())
		{
			if (child is TowerDefensePlantPot)
			{
				num++;
			}
		}
		return num;
	}

	private void RegisterPotCharacter()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (instance.TOWERDEFENSE_CHARCATERS.TryGetValue("PlantPot", out var value))
		{
			_previousPotCharacter = value;
		}
		else
		{
			_potCharacterWasMissing = true;
		}
		instance.TOWERDEFENSE_CHARCATERS["PlantPot"] = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/Pot/Scene/TowerDefensePlantPot.tscn", null, ResourceLoader.CacheMode.Ignore);
		if (instance.CHARCTAER_SPRITE.TryGetValue("PlantPot", out var value2))
		{
			_previousPotSprite = value2;
		}
		else
		{
			_potSpriteWasMissing = true;
		}
		instance.CHARCTAER_SPRITE["PlantPot"] = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/Pot/Pot.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
	}

	private void RestorePotCharacter()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			if (_potCharacterWasMissing)
			{
				instance.TOWERDEFENSE_CHARCATERS.Remove("PlantPot");
			}
			else if (GodotObject.IsInstanceValid(_previousPotCharacter))
			{
				instance.TOWERDEFENSE_CHARCATERS["PlantPot"] = _previousPotCharacter;
			}
			if (_potSpriteWasMissing)
			{
				instance.CHARCTAER_SPRITE.Remove("PlantPot");
			}
			else if (GodotObject.IsInstanceValid(_previousPotSprite))
			{
				instance.CHARCTAER_SPRITE["PlantPot"] = _previousPotSprite;
			}
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
			GD.PushError("[BugOverviewMobileRoofFifthRowTapRuntimeTest] " + message);
		}
	}
}
