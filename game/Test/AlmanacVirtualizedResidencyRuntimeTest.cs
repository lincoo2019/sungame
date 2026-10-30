using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class AlmanacVirtualizedResidencyRuntimeTest : Node
{
	private const string ResultMarker = "ALMANAC_VIRTUALIZED_RESIDENCY_RESULT";

	private const string AlmanacScenePath = "res://Prefab/GUI/DialogBox/Almanac/Almanac.tscn";

	private const float PlantColumnStride = 52f;

	private const float ZombieColumnStride = 75f;

	private readonly List<string> _failures = new List<string>();

	public override async void _Ready()
	{
		ResourceManager resourceManager = ResourceManager.Instance;
		if (!GodotObject.IsInstanceValid(resourceManager))
		{
			Fail("ResourceManager is unavailable");
			Finish(0, 0, 0, 0);
			return;
		}
		resourceManager.BeginLoad();
		for (int frame = 0; frame < 7200; frame++)
		{
			if (resourceManager.CurrentGameplayResourceLoadState == GameplayResourceLoadState.Ready)
			{
				break;
			}
			if (resourceManager.CurrentGameplayResourceLoadState == GameplayResourceLoadState.Failed)
			{
				break;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		if (!resourceManager.AreFullGameplayResourcesReady)
		{
			Fail($"Full gameplay readiness failed: state={resourceManager.CurrentGameplayResourceLoadState}, error={resourceManager.FullGameplayResourceLoadError}");
			Finish(0, 0, 0, resourceManager.LateCharacterResourceLoadCount);
			return;
		}
		int lateLoadCountBefore = resourceManager.LateCharacterResourceLoadCount;
		PackedScene packedScene = GD.Load<PackedScene>("res://Prefab/GUI/DialogBox/Almanac/Almanac.tscn");
		Check(GodotObject.IsInstanceValid(packedScene), "Unable to load Almanac scene: res://Prefab/GUI/DialogBox/Almanac/Almanac.tscn");
		if (!GodotObject.IsInstanceValid(packedScene))
		{
			Finish(0, 0, 0, resourceManager.LateCharacterResourceLoadCount);
			return;
		}
		Almanac almanac = packedScene.Instantiate<Almanac>(PackedScene.GenEditState.Disabled);
		AddChild(almanac, forceReadableName: false, InternalMode.Disabled);
		await WaitProcessFrames(3);
		Check(almanac.PlantLogicalEntryCount == 0 && almanac.ZombieLogicalEntryCount == 0, "Almanac eagerly initialized a hidden category");
		Check(almanac.PlantPreviewNodeCount == 0 && almanac.ZombiePreviewNodeCount == 0, "Almanac eagerly instantiated hidden preview nodes");
		almanac.plantShowAll = true;
		almanac.PlantButtonPressed();
		await WaitForVirtualBindingsAsync(almanac, plant: true);
		int plantNodes = almanac.PlantPreviewNodeCount;
		Check(almanac.PlantLogicalEntryCount > 0, "Plant logical catalog is empty");
		Check(plantNodes > 0 && plantNodes <= almanac.PlantPreviewNodeLimit, $"Plant preview pool is unbounded: nodes={plantNodes}, limit={almanac.PlantPreviewNodeLimit}");
		float plantAvailableWidth = GetExpectedVirtualContentWidth(almanac.plantPacketScroll, almanac.packetMargin);
		Check(Mathf.IsZeroApprox(almanac.plantPacketContainer.CustomMinimumSize.X), "Plant virtual container incorrectly reserves a horizontal scroll width");
		int num = Math.Max(1, Mathf.FloorToInt(plantAvailableWidth / 52f) + 1);
		int num2 = CountVisiblePreviewColumns<TowerDefenseInGamePacketShow>(almanac.plantPacketContainer);
		float plantTrailingSpace = plantAvailableWidth - (float)(almanac.PlantColumnCount - 1) * 52f;
		Check(almanac.PlantColumnCount == num && almanac.PlantColumnCount > 1, $"Plant virtual grid lost a viewport column: actual={almanac.PlantColumnCount}, expected={num}, width={plantAvailableWidth}");
		Check(num2 == almanac.PlantColumnCount, $"Plant initial bindings do not fill every calculated column: visible={num2}, columns={almanac.PlantColumnCount}");
		Check(plantTrailingSpace >= -1f && plantTrailingSpace < 52f, $"Plant grid leaves a full blank card slot on the right: trailing={plantTrailingSpace}, stride={52f}");
		TowerDefenseInGamePacketShow plantPreview = FindFirstDescendant<TowerDefenseInGamePacketShow>(almanac.plantPacketContainer);
		Check(GodotObject.IsInstanceValid(plantPreview?.sprite), "Plant Almanac did not create a real Adobe Animate preview");
		if (GodotObject.IsInstanceValid(plantPreview?.sprite))
		{
			int frame = plantPreview.sprite.frameIndex;
			plantPreview.OnMouseEntered();
			await WaitProcessFrames(5);
			Check(plantPreview.sprite.frameIndex != frame || plantPreview.sprite.frameRate > 0.0, "Plant hover preview did not enter real animation playback");
			plantPreview.OnMouseExited();
		}
		GD.Print($"ALMANAC_VIRTUALIZED_RESIDENCY_PHASE plant-initialized columns={almanac.PlantColumnCount} contentWidth={plantAvailableWidth:F2} trailing={plantTrailingSpace:F2}");
		ScrollToBottom(almanac.plantPacketScroll);
		almanac.RefreshPlantVirtualBindings();
		await WaitForVirtualBindingsAsync(almanac, plant: true);
		Math.Max(plantNodes, almanac.PlantPreviewNodeCount);
		Check(almanac.PlantPreviewNodeCount <= almanac.PlantPreviewNodeLimit, "Plant scrolling exceeded the calculated node limit");
		Check(almanac.PlantPreviewNodeCount <= almanac.PlantLogicalEntryCount, "Plant scrolling created more nodes than logical entries");
		int largePlantNodeCount = almanac.PlantPreviewNodeCount;
		int largePlantNodeLimit = almanac.PlantPreviewNodeLimit;
		int largePlantAnimationCount = CountDescendants<AdobeAnimateSprite>(almanac.plantPacketContainer);
		GD.Print("ALMANAC_VIRTUALIZED_RESIDENCY_PHASE plant-scrolled");
		almanac.ZombieButtonPressed();
		await WaitForVirtualBindingsAsync(almanac, plant: false);
		int zombieNodes = almanac.ZombiePreviewNodeCount;
		Check(almanac.ZombieLogicalEntryCount > 0, "Zombie logical catalog is empty");
		Check(zombieNodes > 0 && zombieNodes <= almanac.ZombiePreviewNodeLimit, $"Zombie preview pool is unbounded: nodes={zombieNodes}, limit={almanac.ZombiePreviewNodeLimit}");
		MarginContainer parent = almanac.zombiePacketContainer.GetParent<MarginContainer>();
		float zombieAvailableWidth = GetExpectedVirtualContentWidth(almanac.zombiePacketScroll, parent);
		Check(Mathf.IsZeroApprox(almanac.zombiePacketContainer.CustomMinimumSize.X), "Zombie virtual container incorrectly reserves a horizontal scroll width");
		int num3 = Math.Max(1, Mathf.FloorToInt(zombieAvailableWidth / 75f) + 1);
		int num4 = CountVisiblePreviewColumns<AlmanacZombieWidow>(almanac.zombiePacketContainer);
		float zombieTrailingSpace = zombieAvailableWidth - (float)(almanac.ZombieColumnCount - 1) * 75f;
		Check(almanac.ZombieColumnCount == num3 && almanac.ZombieColumnCount > 1, $"Zombie virtual grid initialized as a single column: actual={almanac.ZombieColumnCount}, expected={num3}, width={zombieAvailableWidth}");
		Check(num4 == almanac.ZombieColumnCount, $"Zombie initial bindings do not fill every calculated column: visible={num4}, columns={almanac.ZombieColumnCount}");
		Check(zombieTrailingSpace >= -1f && zombieTrailingSpace < 75f, $"Zombie grid leaves a full blank card slot on the right: trailing={zombieTrailingSpace}, stride={75f}");
		AlmanacZombieWidow zombiePreview = FindFirstDescendant<AlmanacZombieWidow>(almanac.zombiePacketContainer);
		Check(GodotObject.IsInstanceValid(zombiePreview?.sprite), "Zombie Almanac did not create a real Adobe Animate preview");
		if (GodotObject.IsInstanceValid(zombiePreview?.sprite))
		{
			int frame = zombiePreview.sprite.frameIndex;
			zombiePreview._MouseEntered();
			await WaitProcessFrames(5);
			Check(zombiePreview.sprite.frameIndex != frame || zombiePreview.sprite.frameRate > 0.0, "Zombie hover preview did not enter real animation playback");
			zombiePreview._MouseExited();
		}
		GD.Print($"ALMANAC_VIRTUALIZED_RESIDENCY_PHASE zombie-initialized columns={almanac.ZombieColumnCount} contentWidth={zombieAvailableWidth:F2} trailing={zombieTrailingSpace:F2}");
		ScrollToBottom(almanac.zombiePacketScroll);
		almanac.RefreshZombieVirtualBindings();
		await WaitForVirtualBindingsAsync(almanac, plant: false);
		Math.Max(zombieNodes, almanac.ZombiePreviewNodeCount);
		Check(almanac.ZombiePreviewNodeCount <= almanac.ZombiePreviewNodeLimit, "Zombie scrolling exceeded the calculated node limit");
		Check(almanac.ZombiePreviewNodeCount <= almanac.ZombieLogicalEntryCount, "Zombie scrolling created more nodes than logical entries");
		int largeZombieNodeCount = almanac.ZombiePreviewNodeCount;
		int largeZombieNodeLimit = almanac.ZombiePreviewNodeLimit;
		int largeZombieAnimationCount = CountDescendants<AdobeAnimateSprite>(almanac.zombiePacketContainer);
		GD.Print("ALMANAC_VIRTUALIZED_RESIDENCY_PHASE zombie-scrolled");
		Control plantLayoutRoot = almanac.GetNode<Control>("PlantLayer/Plant");
		Control zombieLayoutRoot = almanac.GetNode<Control>("ZombieLayer/Zombie");
		Vector2 largeViewportSize = plantLayoutRoot.Size;
		plantLayoutRoot.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		zombieLayoutRoot.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		plantLayoutRoot.Size = new Vector2(720f, plantLayoutRoot.Size.Y);
		zombieLayoutRoot.Size = new Vector2(720f, zombieLayoutRoot.Size.Y);
		await WaitProcessFrames(4);
		await WaitForVirtualBindingsAsync(almanac, plant: true);
		await WaitForVirtualBindingsAsync(almanac, plant: false);
		await WaitProcessFrames(3);
		int plantPreviewNodeLimit = almanac.PlantPreviewNodeLimit;
		int zombiePreviewNodeLimit = almanac.ZombiePreviewNodeLimit;
		int num5 = CountDescendants<AdobeAnimateSprite>(almanac.plantPacketContainer);
		int num6 = CountDescendants<AdobeAnimateSprite>(almanac.zombiePacketContainer);
		Check(plantPreviewNodeLimit < largePlantNodeLimit, $"Plant node limit did not shrink with the real viewport: large={largePlantNodeLimit}, small={plantPreviewNodeLimit}, layout={largeViewportSize}->{plantLayoutRoot.Size}");
		Check(zombiePreviewNodeLimit < largeZombieNodeLimit, $"Zombie node limit did not shrink with the real viewport: large={largeZombieNodeLimit}, small={zombiePreviewNodeLimit}, layout={largeViewportSize}->{zombieLayoutRoot.Size}");
		Check(almanac.PlantPreviewNodeCount < largePlantNodeCount && almanac.PlantPreviewNodeCount <= plantPreviewNodeLimit, $"Plant pool was not clipped after viewport shrink: large={largePlantNodeCount}, current={almanac.PlantPreviewNodeCount}, limit={plantPreviewNodeLimit}");
		Check(almanac.ZombiePreviewNodeCount < largeZombieNodeCount && almanac.ZombiePreviewNodeCount <= zombiePreviewNodeLimit, $"Zombie pool was not clipped after viewport shrink: large={largeZombieNodeCount}, current={almanac.ZombiePreviewNodeCount}, limit={zombiePreviewNodeLimit}");
		Check(CountDescendants<TowerDefenseInGamePacketShow>(almanac.plantPacketContainer) == almanac.PlantPreviewNodeCount, "Plant pool clipping left queued preview nodes in the scene tree");
		Check(CountDescendants<AlmanacZombieWidow>(almanac.zombiePacketContainer) == almanac.ZombiePreviewNodeCount, "Zombie pool clipping left queued preview windows in the scene tree");
		Check(num5 < largePlantAnimationCount, $"Plant pool clipping did not release real animation trees: large={largePlantAnimationCount}, small={num5}");
		Check(num6 < largeZombieAnimationCount, $"Zombie pool clipping did not release real animation trees: large={largeZombieAnimationCount}, small={num6}");
		plantNodes = almanac.PlantPreviewNodeCount;
		zombieNodes = almanac.ZombiePreviewNodeCount;
		GD.Print($"ALMANAC_VIRTUALIZED_RESIDENCY_PHASE viewport-shrunk plantNodes={plantNodes}/{plantPreviewNodeLimit} zombieNodes={zombieNodes}/{zombiePreviewNodeLimit}");
		almanac.PropButtonPressed();
		await WaitProcessFrames(2);
		int propNodes = almanac.propShovelContainer.GetChildCount() + almanac.propMowerContainer.GetChildCount();
		Check(almanac.IsPropCategoryInitialized, "Prop Almanac did not perform its lazy category initialization");
		almanac.PropButtonPressed();
		await WaitProcessFrames(1);
		Check(propNodes == almanac.propShovelContainer.GetChildCount() + almanac.propMowerContainer.GetChildCount(), "Reopening the prop category duplicated item nodes");
		GD.Print("ALMANAC_VIRTUALIZED_RESIDENCY_PHASE prop-initialized");
		for (int frame = 0; frame < 3; frame++)
		{
			almanac.IndexButtonPressed();
			almanac.PlantButtonPressed();
			almanac.ZombieButtonPressed();
			ScrollToTop(almanac.plantPacketScroll);
			ScrollToBottom(almanac.zombiePacketScroll);
			almanac.RefreshPlantVirtualBindings();
			almanac.RefreshZombieVirtualBindings();
			await WaitForVirtualBindingsAsync(almanac, plant: true);
			await WaitForVirtualBindingsAsync(almanac, plant: false);
			plantNodes = Math.Max(plantNodes, almanac.PlantPreviewNodeCount);
			zombieNodes = Math.Max(zombieNodes, almanac.ZombiePreviewNodeCount);
		}
		Check(plantNodes <= almanac.PlantPreviewNodeLimit && plantNodes <= almanac.PlantLogicalEntryCount, "Repeated category switching exceeded the plant node bound");
		Check(zombieNodes <= almanac.ZombiePreviewNodeLimit && zombieNodes <= almanac.ZombieLogicalEntryCount, "Repeated category switching exceeded the zombie node bound");
		Check(resourceManager.LateCharacterResourceLoadCount == lateLoadCountBefore, $"Almanac caused a late character resource load: before={lateLoadCountBefore}, after={resourceManager.LateCharacterResourceLoadCount}");
		GD.Print("ALMANAC_VIRTUALIZED_RESIDENCY_PHASE interactions-complete");
		int plantLogicalCount = almanac.PlantLogicalEntryCount;
		int zombieLogicalCount = almanac.ZombieLogicalEntryCount;
		await WaitPhysicsFrames(5);
		GD.Print("ALMANAC_VIRTUALIZED_RESIDENCY_PHASE callbacks-settled");
		almanac.QueueFree();
		await WaitProcessFrames(3);
		await WaitPhysicsFrames(3);
		Finish(plantLogicalCount, zombieLogicalCount, plantNodes + zombieNodes + propNodes, resourceManager.LateCharacterResourceLoadCount);
	}

	private async Task WaitProcessFrames(int frameCount)
	{
		for (int frame = 0; frame < frameCount; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private async Task WaitPhysicsFrames(int frameCount)
	{
		for (int frame = 0; frame < frameCount; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task WaitForVirtualBindingsAsync(Almanac almanac, bool plant)
	{
		for (int frame = 0; frame < 240; frame++)
		{
			if (plant ? almanac.ArePlantVirtualBindingsStable : almanac.AreZombieVirtualBindingsStable)
			{
				await WaitProcessFrames(2);
				if (plant ? almanac.ArePlantVirtualBindingsStable : almanac.AreZombieVirtualBindingsStable)
				{
					return;
				}
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		Fail(plant ? "Plant virtual bindings did not settle" : "Zombie virtual bindings did not settle");
	}

	private static void ScrollToBottom(ScrollContainer scroll)
	{
		ScrollBar vScrollBar = scroll.GetVScrollBar();
		scroll.ScrollVertical = Mathf.RoundToInt(vScrollBar.MaxValue);
	}

	private static void ScrollToTop(ScrollContainer scroll)
	{
		scroll.ScrollVertical = 0;
	}

	private static float GetExpectedVirtualContentWidth(ScrollContainer scroll, MarginContainer margin)
	{
		float num = scroll.Size.X;
		ScrollBar vScrollBar = scroll.GetVScrollBar();
		if (GodotObject.IsInstanceValid(vScrollBar) && vScrollBar.Visible)
		{
			num -= vScrollBar.Size.X;
		}
		num -= (float)(margin.GetThemeConstant("margin_left") + margin.GetThemeConstant("margin_right"));
		return Math.Max(0f, num);
	}

	private static int CountVisiblePreviewColumns<T>(Control container) where T : Control
	{
		HashSet<int> hashSet = new HashSet<int>();
		foreach (Node child in container.GetChildren())
		{
			if (child is T val && val.Visible)
			{
				hashSet.Add(Mathf.RoundToInt(val.Position.X));
			}
		}
		return hashSet.Count;
	}

	private static T FindFirstDescendant<T>(Node root) where T : Node
	{
		foreach (Node child in root.GetChildren(includeInternal: true))
		{
			if (child is T result)
			{
				return result;
			}
			T val = FindFirstDescendant<T>(child);
			if (GodotObject.IsInstanceValid(val))
			{
				return val;
			}
		}
		return null;
	}

	private static int CountDescendants<T>(Node root) where T : Node
	{
		int num = 0;
		foreach (Node child in root.GetChildren(includeInternal: true))
		{
			if (child is T)
			{
				num++;
			}
			num += CountDescendants<T>(child);
		}
		return num;
	}

	private void Check(bool condition, string message)
	{
		if (!condition)
		{
			Fail(message);
		}
	}

	private void Fail(string message)
	{
		_failures.Add(message);
		GD.PrintErr("ALMANAC_VIRTUALIZED_RESIDENCY_FAILURE " + message);
	}

	private void Finish(int plantLogicalCount, int zombieLogicalCount, int previewNodeCount, int lateLoadCount)
	{
		GD.Print($"{"ALMANAC_VIRTUALIZED_RESIDENCY_RESULT"} passed={_failures.Count == 0} plantLogical={plantLogicalCount} zombieLogical={zombieLogicalCount} previewNodes={previewNodeCount} lateCharacterResourceLoadCount={lateLoadCount} failures={_failures.Count}");
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
