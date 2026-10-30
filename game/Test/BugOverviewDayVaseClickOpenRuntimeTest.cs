using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewDayVaseClickOpenRuntimeTest : Node
{
	private const string ScenePath = "res://Asset/Anime/Character/Plant/Star/DayVase/Scene/TowerDefensePlantDayVase.tscn";

	private const string PacketPath = "res://Asset/Anime/Character/Plant/Star/DayVase/Packet/PlantDayVase.tres";

	private const string NormalVaseScenePath = "res://Asset/Anime/Character/Vase/Normal/Scene/TowerDefenseVaseNormal.tscn";

	private const string NormalVasePacketPath = "res://Asset/Anime/Character/Vase/Normal/Packet/VaseNormal.tres";

	private const string PacketShowScenePath = "res://Prefab/TowerDefense/GUI/InGame/Packet/TowerDefenseInGamePacketShow.tscn";

	private const string MapControlPath = "res://Registry/Battle/Feature/Map/Control/TowerDefenseMapControl.tscn";

	private const string FrontlawnMapPath = "res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawn.tres";

	private const string FrontlawnNightMapPath = "res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawnNight.tres";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		DayVaseClickOpenRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseVaseNormal coveredVase = null;
		TowerDefenseVaseNormal upperVase = null;
		TowerDefenseVaseNormal lowerVase = null;
		TowerDefensePlantDayVase dayVase = null;
		TowerDefensePlantDayVase switchingDayVase = null;
		CanvasLayer packetLayer = null;
		TowerDefenseInGamePacketShow droppedPacket = null;
		PackedScene scene = null;
		PackedScene normalVaseScene = null;
		PackedScene packetShowScene = null;
		PackedScene mapControlScene = null;
		Dictionary<string, Resource> previousMaps = new Dictionary<string, Resource>();
		HashSet<string> missingMaps = new HashSet<string>();
		Dictionary<string, Resource> previousPackets = new Dictionary<string, Resource>();
		HashSet<string> missingPackets = new HashSet<string>();
		Dictionary<string, Resource> previousCharacters = new Dictionary<string, Resource>();
		HashSet<string> missingCharacters = new HashSet<string>();
		try
		{
			_ = 20;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_01fa;
				}
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				control = new DayVaseClickOpenRuntimeControlStub
				{
					Name = "DayVaseClickOpenRuntimeControl",
					isGameRunning = true,
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
				manager.gridBeginPos = new Vector2(260f, 75f);
				manager.gridSize = new Vector2(80f, 98f);
				manager.gridNum = new Vector2I(9, 5);
				TowerDefenseMapConfig frontlawnMap = ResourceLoader.Load<TowerDefenseMapConfig>("res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawn.tres", null, ResourceLoader.CacheMode.Ignore);
				TowerDefenseMapConfig towerDefenseMapConfig = ResourceLoader.Load<TowerDefenseMapConfig>("res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawnNight.tres", null, ResourceLoader.CacheMode.Ignore);
				RegisterMap("Frontlawn", frontlawnMap, previousMaps, missingMaps);
				RegisterMap("FrontlawnNight", towerDefenseMapConfig, previousMaps, missingMaps);
				mapControlScene = ResourceLoader.Load<PackedScene>("res://Registry/Battle/Feature/Map/Control/TowerDefenseMapControl.tscn", null, ResourceLoader.CacheMode.Ignore);
				mapControl = mapControlScene?.Instantiate<TowerDefenseMapControl>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(frontlawnMap) && GodotObject.IsInstanceValid(towerDefenseMapConfig) && GodotObject.IsInstanceValid(mapControl), "The real Frontlawn day/night configs and map control must load.");
				if (!GodotObject.IsInstanceValid(frontlawnMap) || !GodotObject.IsInstanceValid(towerDefenseMapConfig) || !GodotObject.IsInstanceValid(mapControl))
				{
					goto end_IL_01fa;
				}
				control.AddChild(mapControl, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(2);
				mapFeature = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
				{
					mapControl = mapControl,
					control = control,
					groundRect = new Rect2(-1000f, -1000f, 3000f, 3000f)
				});
				control.featureDictionary[new StringName("Map")] = mapFeature;
				Check(mapFeature.MapInit(frontlawnMap) && GodotObject.IsInstanceValid(mapFeature.currentMap), "The focused fixture must initialize the real Frontlawn runtime map.");
				TowerDefensePacketConfig dayVasePacket = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Star/DayVase/Packet/PlantDayVase.tres", null, ResourceLoader.CacheMode.Ignore);
				scene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Star/DayVase/Scene/TowerDefensePlantDayVase.tscn", null, ResourceLoader.CacheMode.Ignore);
				TowerDefensePacketConfig normalVasePacket = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Vase/Normal/Packet/VaseNormal.tres", null, ResourceLoader.CacheMode.Ignore);
				normalVaseScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Vase/Normal/Scene/TowerDefenseVaseNormal.tscn", null, ResourceLoader.CacheMode.Ignore);
				RegisterResource(ResourceManager.Instance.TOWERDEFENSE_PACKETS, "PlantDayVase", dayVasePacket, previousPackets, missingPackets);
				RegisterResource(ResourceManager.Instance.TOWERDEFENSE_CHARCATERS, "PlantDayVase", scene, previousCharacters, missingCharacters);
				RegisterResource(ResourceManager.Instance.TOWERDEFENSE_PACKETS, "VaseNormal", normalVasePacket, previousPackets, missingPackets);
				RegisterResource(ResourceManager.Instance.TOWERDEFENSE_CHARCATERS, "VaseNormal", normalVaseScene, previousCharacters, missingCharacters);
				Check(GodotObject.IsInstanceValid(dayVasePacket) && GodotObject.IsInstanceValid(scene), "The real DayVase packet and scene must load.");
				Check(GodotObject.IsInstanceValid(normalVasePacket) && GodotObject.IsInstanceValid(normalVaseScene), "The real ordinary vase packet and scene must load.");
				coveredVase = normalVasePacket?.Plant(new Vector2I(1, 2), playAudio: false) as TowerDefenseVaseNormal;
				Check(GodotObject.IsInstanceValid(coveredVase), "A real ordinary vase must exist below the dropped card input probe.");
				if (!GodotObject.IsInstanceValid(coveredVase))
				{
					goto end_IL_01fa;
				}
				await WaitFrames(8);
				MousePressComponent mousePressComponent = coveredVase.componentManager?.GetRuntime<MousePressComponent>();
				Vector2 coveredClickCenter = Vector2.Zero;
				bool flag = mousePressComponent != null && !mousePressComponent.IsReleased && mousePressComponent.Lifecycle == ComponentRuntimeLifecycle.Active && mousePressComponent.TryGetClickShapeScreenCenter(out coveredClickCenter);
				Check(flag, "The covered ordinary vase must expose its real active click area.");
				if (!flag)
				{
					goto end_IL_01fa;
				}
				packetShowScene = ResourceLoader.Load<PackedScene>("res://Prefab/TowerDefense/GUI/InGame/Packet/TowerDefenseInGamePacketShow.tscn", null, ResourceLoader.CacheMode.Ignore);
				droppedPacket = packetShowScene?.Instantiate<TowerDefenseInGamePacketShow>(PackedScene.GenEditState.Disabled);
				packetLayer = new CanvasLayer
				{
					Name = "DroppedPacketInputLayer",
					Layer = 100
				};
				AddChild(packetLayer, forceReadableName: false, InternalMode.Disabled);
				if (GodotObject.IsInstanceValid(droppedPacket))
				{
					droppedPacket.config = dayVasePacket;
					droppedPacket.useCost = false;
					droppedPacket.Position = coveredClickCenter;
					packetLayer.AddChild(droppedPacket, forceReadableName: false, InternalMode.Disabled);
				}
				await WaitFrames(2);
				Check(GodotObject.IsInstanceValid(droppedPacket?.button), "The real dropped packet and its production Button must enter the front CanvasLayer.");
				if (!GodotObject.IsInstanceValid(droppedPacket?.button))
				{
					goto end_IL_01fa;
				}
				droppedPacket.Position += coveredClickCenter - droppedPacket.button.GetGlobalRect().GetCenter();
				await WaitFrames(2);
				bool packetButtonPressed = false;
				droppedPacket.button.Pressed += delegate
				{
					packetButtonPressed = true;
				};
				await PushInput(new InputEventMouseMotion
				{
					Position = coveredClickCenter
				});
				Check(droppedPacket.button.GetGlobalRect().HasPoint(coveredClickCenter) && GodotObject.IsInstanceValid(GetViewport().GuiGetHoveredControl()), "The synthesized pointer must be over the real dropped-card Button before clicking.");
				await PushInput(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					ButtonMask = MouseButtonMask.Left,
					Pressed = true,
					Position = coveredClickCenter,
					GlobalPosition = coveredClickCenter
				});
				await PushInput(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					ButtonMask = (MouseButtonMask)0L,
					Pressed = false,
					Position = coveredClickCenter,
					GlobalPosition = coveredClickCenter
				});
				Check(packetButtonPressed, "Picking the dropped card must still reach the real front Button.");
				Check(!coveredVase.pressed, "Picking a dropped card above an ordinary vase must not also press and break the vase behind it.");
				coveredVase.QueueFree();
				coveredVase = null;
				droppedPacket.QueueFree();
				droppedPacket = null;
				packetLayer.QueueFree();
				packetLayer = null;
				await WaitFrames(3);
				upperVase = normalVasePacket.Plant(new Vector2I(4, 2), playAudio: false) as TowerDefenseVaseNormal;
				lowerVase = normalVasePacket.Plant(new Vector2I(4, 3), playAudio: false) as TowerDefenseVaseNormal;
				Check(GodotObject.IsInstanceValid(upperVase) && GodotObject.IsInstanceValid(lowerVase), "Two vertically adjacent ordinary vases must spawn in their real map cells.");
				if (!GodotObject.IsInstanceValid(upperVase) || !GodotObject.IsInstanceValid(lowerVase))
				{
					goto end_IL_01fa;
				}
				await WaitFrames(8);
				MousePressComponent mousePressComponent2 = upperVase.componentManager?.GetRuntime<MousePressComponent>();
				MousePressComponent mousePressComponent3 = lowerVase.componentManager?.GetRuntime<MousePressComponent>();
				Rect2 worldRect = default(Rect2);
				Rect2 worldRect2 = default(Rect2);
				bool flag2 = mousePressComponent2 != null && !mousePressComponent2.IsReleased && mousePressComponent2.TryGetClickShapeWorldRect(out worldRect);
				bool flag3 = mousePressComponent3 != null && !mousePressComponent3.IsReleased && mousePressComponent3.TryGetClickShapeWorldRect(out worldRect2);
				Rect2 rect = ((flag2 && flag3) ? worldRect.Intersection(worldRect2) : default(Rect2));
				Check(flag2 && flag3 && rect.HasArea(), "Adjacent ordinary vase click shapes must expose the reported vertical overlap.");
				if (!rect.HasArea())
				{
					goto end_IL_01fa;
				}
				Vector2 vector = rect.GetCenter() + new Vector2(0f, 1f);
				Vector2 overlapScreenPosition = upperVase.GetCanvasTransform() * vector;
				Check(mousePressComponent2.IsScreenPositionInsideClickShape(overlapScreenPosition) && mousePressComponent3.IsScreenPositionInsideClickShape(overlapScreenPosition), "The synthesized click must hit both authored vase click shapes before target arbitration.");
				Vector2I mapGridPosFromMouse = manager.GetMapGridPosFromMouse(vector);
				TowerDefenseVase selectedVase = ((mapGridPosFromMouse == upperVase.gridPos) ? upperVase : ((mapGridPosFromMouse == lowerVase.gridPos) ? lowerVase : null));
				TowerDefenseVase unselectedVase = ((selectedVase == upperVase) ? lowerVase : upperVase);
				Check(GodotObject.IsInstanceValid(selectedVase), "The overlap point must resolve to exactly one real map cell.");
				await PushInput(new InputEventMouseMotion
				{
					Position = overlapScreenPosition
				});
				await PushInput(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					ButtonMask = MouseButtonMask.Left,
					Pressed = true,
					Position = overlapScreenPosition,
					GlobalPosition = overlapScreenPosition
				});
				await PushInput(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					ButtonMask = (MouseButtonMask)0L,
					Pressed = false,
					Position = overlapScreenPosition,
					GlobalPosition = overlapScreenPosition
				});
				await WaitFrames(2);
				int num = (upperVase.pressed ? 1 : 0) + (lowerVase.pressed ? 1 : 0);
				Check(num == 1, "One click in overlapping vase shapes must open exactly one vase.");
				Check(GodotObject.IsInstanceValid(selectedVase) && selectedVase.pressed, "The vase in the pointer's resolved map cell must receive the click.");
				Check(GodotObject.IsInstanceValid(unselectedVase) && !unselectedVase.pressed, "The adjacent vase outside the resolved map cell must remain closed.");
				upperVase.QueueFree();
				lowerVase.QueueFree();
				upperVase = null;
				lowerVase = null;
				await WaitFrames(3);
				dayVase = dayVasePacket?.Plant(new Vector2I(2, 2), playAudio: false) as TowerDefensePlantDayVase;
				Check(GodotObject.IsInstanceValid(dayVase), "The real DayVase packet must plant its authored character in the runtime map cell.");
				if (!GodotObject.IsInstanceValid(dayVase))
				{
					goto end_IL_01fa;
				}
				await WaitFrames(8);
				Check((TowerDefenseManager.GetMapCell(new Vector2I(2, 2))?.HasCharacter("PlantDayVase") ?? false) && dayVase.inGame, "The planted DayVase must occupy its authored cell before clicking.");
				AdobeAnimateSpriteBase hammer = dayVase.GetNodeOrNull<AdobeAnimateSpriteBase>("%Hammer");
				MousePressComponent mousePressComponent4 = dayVase.componentManager?.GetRuntime<MousePressComponent>();
				Check(GodotObject.IsInstanceValid(hammer), "The real DayVase Hammer animation node must exist.");
				Check(mousePressComponent4 != null && !mousePressComponent4.IsReleased && mousePressComponent4.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(mousePressComponent4.ClickShape), "The real DayVase MousePress runtime and click shape must be active.");
				if (!GodotObject.IsInstanceValid(hammer) || mousePressComponent4 == null || mousePressComponent4.IsReleased)
				{
					goto end_IL_01fa;
				}
				Check(mousePressComponent4.TryGetClickShapeScreenCenter(out var screenCenter), "The DayVase click shape must expose a finite screen center.");
				Check(dayVase.componentManager.HasRuntimeInputWork && mousePressComponent4.HasRuntimeInputWork && mousePressComponent4.CanDispatchInputWork, "ComponentManager must dispatch the active DayVase MousePress runtime.");
				Check(manager.IsGameRunning() && dayVase.inGame && dayVase.componentAlive && mousePressComponent4.Alive, "DayVase and MousePress must be interactable during the running fixture.");
				Check(mousePressComponent4.IsScreenPositionInsideClickShape(screenCenter), "The synthesized click must resolve inside DayVase's configured click shape.");
				bool destroyObserved = false;
				dayVase.OnDestroy += delegate
				{
					destroyObserved = true;
				};
				DispatchMouse(dayVase.componentManager, screenCenter, pressed: true);
				DispatchMouse(dayVase.componentManager, screenCenter, pressed: false);
				await WaitFrames(2);
				Check(dayVase.pressed && hammer.Visible, "A real click must mark DayVase pressed and start the hammer opening animation.");
				await WaitFrames(180);
				Check(destroyObserved && dayVase.isDestroy, "The real Hammer OpenPot animation must complete and enter the DayVase destroy lifecycle.");
				Check(mapFeature.isChange || (mapFeature.config?.isNight ?? false), "Opening DayVase must request the real Frontlawn day/night transition.");
				switchingDayVase = dayVasePacket.Plant(new Vector2I(3, 2), playAudio: false) as TowerDefensePlantDayVase;
				Check(GodotObject.IsInstanceValid(switchingDayVase), "A second DayVase must plant while the map transition is active.");
				if (!GodotObject.IsInstanceValid(switchingDayVase))
				{
					goto end_IL_01fa;
				}
				await WaitFrames(8);
				mapFeature.isChange = true;
				AdobeAnimateSpriteBase switchingHammer = switchingDayVase.GetNodeOrNull<AdobeAnimateSpriteBase>("%Hammer");
				MousePressComponent mousePressComponent5 = switchingDayVase.componentManager?.GetRuntime<MousePressComponent>();
				Vector2 screenCenter2 = Vector2.Zero;
				bool flag4 = mousePressComponent5 != null && !mousePressComponent5.IsReleased && mousePressComponent5.TryGetClickShapeScreenCenter(out screenCenter2);
				Check(flag4, "The second DayVase MousePress runtime must expose a click center.");
				if (flag4 && GodotObject.IsInstanceValid(switchingHammer))
				{
					bool switchingDestroyObserved = false;
					switchingDayVase.OnDestroy += delegate
					{
						switchingDestroyObserved = true;
					};
					DispatchMouse(switchingDayVase.componentManager, screenCenter2, pressed: true);
					DispatchMouse(switchingDayVase.componentManager, screenCenter2, pressed: false);
					await WaitFrames(2);
					Check(switchingDayVase.pressed && switchingHammer.Visible, "DayVase must still start opening when the map is already transitioning.");
					switchingDayVase.HammerAnimeCompleted("OpenPot");
					await WaitFrames(8);
					Check(switchingDestroyObserved && switchingDayVase.isDestroy, "DayVase OpenPot completion must still destroy the shell while the map is already transitioning.");
				}
				goto end_IL_019a;
				end_IL_01fa:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewDayVaseClickOpenRuntimeTest] Unexpected exception: {value}");
				goto end_IL_019a;
			}
			return;
			end_IL_019a:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(coveredVase) && !coveredVase.IsQueuedForDeletion())
			{
				coveredVase.QueueFree();
			}
			if (GodotObject.IsInstanceValid(upperVase) && !upperVase.IsQueuedForDeletion())
			{
				upperVase.QueueFree();
			}
			if (GodotObject.IsInstanceValid(lowerVase) && !lowerVase.IsQueuedForDeletion())
			{
				lowerVase.QueueFree();
			}
			if (GodotObject.IsInstanceValid(dayVase) && !dayVase.IsQueuedForDeletion())
			{
				dayVase.QueueFree();
			}
			if (GodotObject.IsInstanceValid(switchingDayVase) && !switchingDayVase.IsQueuedForDeletion())
			{
				switchingDayVase.QueueFree();
			}
			if (GodotObject.IsInstanceValid(droppedPacket) && !droppedPacket.IsQueuedForDeletion())
			{
				droppedPacket.QueueFree();
			}
			if (GodotObject.IsInstanceValid(packetLayer) && !packetLayer.IsQueuedForDeletion())
			{
				packetLayer.QueueFree();
			}
			await WaitFrames(6);
			if (GodotObject.IsInstanceValid(manager))
			{
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
			RestoreResources(ResourceManager.Instance?.TOWERDEFENSE_PACKETS, previousPackets, missingPackets);
			RestoreResources(ResourceManager.Instance?.TOWERDEFENSE_CHARCATERS, previousCharacters, missingCharacters);
			RestoreMaps(previousMaps, missingMaps);
			await WaitFrames(3);
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			scene?.Dispose();
			normalVaseScene?.Dispose();
			packetShowScene?.Dispose();
			mapControlScene?.Dispose();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag5 = _failures == 0 && _checks == 34;
		GD.Print($"DAY_VASE_CLICK_OPEN_RESULT passed={flag5} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag5) ? 2 : 0);
	}

	private static void RegisterResource(Dictionary<string, Resource> registry, string key, Resource resource, Dictionary<string, Resource> previousResources, HashSet<string> missingResources)
	{
		if (registry.TryGetValue(key, out var value))
		{
			previousResources[key] = value;
		}
		else
		{
			missingResources.Add(key);
		}
		registry[key] = resource;
	}

	private static void RestoreResources(Dictionary<string, Resource> registry, Dictionary<string, Resource> previousResources, HashSet<string> missingResources)
	{
		if (registry == null)
		{
			return;
		}
		foreach (string missingResource in missingResources)
		{
			registry.Remove(missingResource);
		}
		foreach (KeyValuePair<string, Resource> previousResource in previousResources)
		{
			registry[previousResource.Key] = previousResource.Value;
		}
	}

	private static void RegisterMap(string key, Resource mapConfig, Dictionary<string, Resource> previousMaps, HashSet<string> missingMaps)
	{
		if (ResourceManager.Instance.MAPS.TryGetValue(key, out var value))
		{
			previousMaps[key] = value;
		}
		else
		{
			missingMaps.Add(key);
		}
		ResourceManager.Instance.MAPS[key] = mapConfig;
	}

	private static void RestoreMaps(Dictionary<string, Resource> previousMaps, HashSet<string> missingMaps)
	{
		if (!GodotObject.IsInstanceValid(ResourceManager.Instance))
		{
			return;
		}
		foreach (string missingMap in missingMaps)
		{
			ResourceManager.Instance.MAPS.Remove(missingMap);
		}
		foreach (KeyValuePair<string, Resource> previousMap in previousMaps)
		{
			ResourceManager.Instance.MAPS[previousMap.Key] = previousMap.Value;
		}
	}

	private static void DispatchMouse(ComponentManager manager, Vector2 screenPosition, bool pressed)
	{
		InputEventMouseButton inputEventMouseButton = new InputEventMouseButton
		{
			ButtonIndex = MouseButton.Left,
			ButtonMask = (MouseButtonMask)(pressed ? 1 : 0),
			Position = screenPosition,
			GlobalPosition = screenPosition,
			Pressed = pressed
		};
		manager._Input(inputEventMouseButton);
		inputEventMouseButton.Dispose();
	}

	private async Task PushInput(InputEvent inputEvent)
	{
		GetViewport().PushInput(inputEvent, inLocalCoords: true);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		inputEvent.Dispose();
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
			GD.PushError("[BugOverviewDayVaseClickOpenRuntimeTest] " + message);
		}
	}
}
